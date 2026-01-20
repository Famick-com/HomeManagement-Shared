using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Famick.HomeManagement.Core.Configuration;
using Famick.HomeManagement.Core.DTOs.Authentication;
using Famick.HomeManagement.Core.DTOs.ExternalAuth;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Domain.Enums;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// Service for handling external authentication providers (Google, Apple, OIDC)
/// </summary>
public class ExternalAuthService : IExternalAuthService
{
    private readonly HomeManagementDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IContactService _contactService;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalAuthSettings _settings;
    private readonly ILogger<ExternalAuthService> _logger;

    private const int StateExpirationMinutes = 10;

    public ExternalAuthService(
        HomeManagementDbContext context,
        ITokenService tokenService,
        IMapper mapper,
        IConfiguration configuration,
        IContactService contactService,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalAuthSettings> settings,
        ILogger<ExternalAuthService> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _mapper = mapper;
        _configuration = configuration;
        _contactService = contactService;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<ExternalAuthProviderDto>> GetEnabledProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = new List<ExternalAuthProviderDto>();

        if (_settings.Google.IsConfigured)
        {
            providers.Add(new ExternalAuthProviderDto
            {
                Provider = "Google",
                DisplayName = "Sign in with Google",
                IsEnabled = true
            });
        }

        if (_settings.Apple.IsConfigured)
        {
            providers.Add(new ExternalAuthProviderDto
            {
                Provider = "Apple",
                DisplayName = "Sign in with Apple",
                IsEnabled = true
            });
        }

        if (_settings.OpenIdConnect.IsConfigured)
        {
            providers.Add(new ExternalAuthProviderDto
            {
                Provider = "OIDC",
                DisplayName = _settings.OpenIdConnect.DisplayName,
                IsEnabled = true
            });
        }

        return Task.FromResult(providers);
    }

    /// <inheritdoc />
    public Task<ExternalAuthChallengeResponse> GetAuthorizationUrlAsync(
        string provider,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        // Generate cryptographically secure state
        var state = GenerateState();

        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Store state and code verifier in cache
        var cacheKey = $"oauth_state_{state}";
        _cache.Set(cacheKey, new OAuthStateData
        {
            Provider = provider,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier
        }, TimeSpan.FromMinutes(StateExpirationMinutes));

        var authUrl = provider.ToUpperInvariant() switch
        {
            "GOOGLE" => BuildGoogleAuthUrl(redirectUri, state, codeChallenge),
            "APPLE" => BuildAppleAuthUrl(redirectUri, state),
            "OIDC" => BuildOidcAuthUrl(redirectUri, state, codeChallenge),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        return Task.FromResult(new ExternalAuthChallengeResponse
        {
            AuthorizationUrl = authUrl,
            State = state
        });
    }

    /// <inheritdoc />
    public Task<ExternalAuthChallengeResponse> GetLinkAuthorizationUrlAsync(
        Guid userId,
        string provider,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        // Generate cryptographically secure state
        var state = GenerateState();

        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Store state, code verifier, and user ID in cache for link operation
        var cacheKey = $"oauth_state_{state}";
        _cache.Set(cacheKey, new OAuthStateData
        {
            Provider = provider,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier,
            LinkUserId = userId,
            IsLinkOperation = true
        }, TimeSpan.FromMinutes(StateExpirationMinutes));

        var authUrl = provider.ToUpperInvariant() switch
        {
            "GOOGLE" => BuildGoogleAuthUrl(redirectUri, state, codeChallenge),
            "APPLE" => BuildAppleAuthUrl(redirectUri, state),
            "OIDC" => BuildOidcAuthUrl(redirectUri, state, codeChallenge),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        return Task.FromResult(new ExternalAuthChallengeResponse
        {
            AuthorizationUrl = authUrl,
            State = state
        });
    }

    /// <inheritdoc />
    public async Task<LoginResponse> ProcessCallbackAsync(
        string provider,
        ExternalAuthCallbackRequest request,
        string redirectUri,
        string ipAddress,
        string deviceInfo,
        CancellationToken cancellationToken = default)
    {
        // Verify state
        var cacheKey = $"oauth_state_{request.State}";
        if (!_cache.TryGetValue<OAuthStateData>(cacheKey, out var stateData))
        {
            _logger.LogWarning("Invalid or expired OAuth state");
            throw new InvalidCredentialsException("Invalid or expired OAuth state");
        }

        // Remove state from cache (one-time use)
        _cache.Remove(cacheKey);

        // Validate provider matches
        if (!stateData!.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("OAuth provider mismatch: expected {Expected}, got {Got}", stateData.Provider, provider);
            throw new InvalidCredentialsException("OAuth provider mismatch");
        }

        // Exchange code for tokens and get user info
        var userInfo = provider.ToUpperInvariant() switch
        {
            "GOOGLE" => await ExchangeGoogleCodeAsync(request.Code, redirectUri, stateData.CodeVerifier, cancellationToken),
            "APPLE" => await ExchangeAppleCodeAsync(request.Code, redirectUri, cancellationToken),
            "OIDC" => await ExchangeOidcCodeAsync(request.Code, redirectUri, stateData.CodeVerifier, cancellationToken),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        // Find or create user
        var user = await FindOrCreateUserAsync(provider, userInfo, cancellationToken);

        // Generate tokens
        return await GenerateLoginResponseAsync(user, ipAddress, deviceInfo, request.RememberMe, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LinkedAccountDto> LinkProviderAsync(
        Guid userId,
        string provider,
        ExternalAuthLinkRequest request,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        // Verify state
        var cacheKey = $"oauth_state_{request.State}";
        if (!_cache.TryGetValue<OAuthStateData>(cacheKey, out var stateData))
        {
            throw new InvalidCredentialsException("Invalid or expired OAuth state");
        }

        _cache.Remove(cacheKey);

        // Get user info from provider
        var userInfo = provider.ToUpperInvariant() switch
        {
            "GOOGLE" => await ExchangeGoogleCodeAsync(request.Code, redirectUri, stateData!.CodeVerifier, cancellationToken),
            "APPLE" => await ExchangeAppleCodeAsync(request.Code, redirectUri, cancellationToken),
            "OIDC" => await ExchangeOidcCodeAsync(request.Code, redirectUri, stateData!.CodeVerifier, cancellationToken),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };

        // Check if this provider account is already linked to another user
        var existingLink = await _context.UserExternalLogins
            .FirstOrDefaultAsync(uel =>
                uel.Provider == provider &&
                uel.ProviderUserId == userInfo.ProviderId &&
                uel.UserId != userId,
                cancellationToken);

        if (existingLink != null)
        {
            throw new DuplicateEntityException("ExternalLogin", "Provider", $"{provider} account is already linked to another user");
        }

        // Check if user already has this provider linked
        var userLink = await _context.UserExternalLogins
            .FirstOrDefaultAsync(uel => uel.UserId == userId && uel.Provider == provider, cancellationToken);

        if (userLink != null)
        {
            throw new DuplicateEntityException("ExternalLogin", "Provider", $"{provider} is already linked to your account");
        }

        // Get user
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        // Create link
        var externalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            UserId = userId,
            Provider = provider,
            ProviderUserId = userInfo.ProviderId,
            ProviderDisplayName = userInfo.Name,
            ProviderEmail = userInfo.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserExternalLogins.Add(externalLogin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Linked {Provider} account to user {UserId}", provider, userId);

        return new LinkedAccountDto
        {
            Id = externalLogin.Id,
            Provider = provider,
            ProviderDisplayName = GetProviderDisplayName(provider),
            ProviderEmail = userInfo.Email,
            LinkedAt = externalLogin.CreatedAt,
            LastUsedAt = null
        };
    }

    /// <inheritdoc />
    public async Task UnlinkProviderAsync(
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var externalLogin = await _context.UserExternalLogins
            .FirstOrDefaultAsync(uel => uel.UserId == userId && uel.Provider == provider, cancellationToken);

        if (externalLogin == null)
        {
            throw new InvalidOperationException($"{provider} is not linked to user {userId}");
        }

        // Check that user still has a way to log in
        var user = await _context.Users
            .Include(u => u.ExternalLogins)
            .Include(u => u.PasskeyCredentials)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        var otherExternalLogins = user.ExternalLogins.Count(el => el.Provider != provider);
        var hasPasskeys = user.PasskeyCredentials.Count > 0;

        if (!hasPassword && otherExternalLogins == 0 && !hasPasskeys)
        {
            throw new InvalidOperationException("Cannot unlink the last authentication method. Please add a password or another login method first.");
        }

        _context.UserExternalLogins.Remove(externalLogin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unlinked {Provider} account from user {UserId}", provider, userId);
    }

    /// <inheritdoc />
    public async Task<List<LinkedAccountDto>> GetLinkedAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var externalLogins = await _context.UserExternalLogins
            .Where(uel => uel.UserId == userId)
            .OrderBy(uel => uel.Provider)
            .ToListAsync(cancellationToken);

        return externalLogins.Select(el => new LinkedAccountDto
        {
            Id = el.Id,
            Provider = el.Provider,
            ProviderDisplayName = GetProviderDisplayName(el.Provider),
            ProviderEmail = el.ProviderEmail,
            LinkedAt = el.CreatedAt,
            LastUsedAt = el.LastUsedAt
        }).ToList();
    }

    #region Helper Methods

    private string BuildGoogleAuthUrl(string redirectUri, string state, string codeChallenge)
    {
        var scopes = "openid email profile";
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(_settings.Google.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&response_type=code"
            + $"&scope={Uri.EscapeDataString(scopes)}"
            + $"&state={Uri.EscapeDataString(state)}"
            + $"&code_challenge={Uri.EscapeDataString(codeChallenge)}"
            + $"&code_challenge_method=S256"
            + $"&access_type=offline"
            + $"&prompt=select_account";

        return authUrl;
    }

    private string BuildAppleAuthUrl(string redirectUri, string state)
    {
        var scopes = "name email";
        var authUrl = "https://appleid.apple.com/auth/authorize"
            + $"?client_id={Uri.EscapeDataString(_settings.Apple.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&response_type=code"
            + $"&scope={Uri.EscapeDataString(scopes)}"
            + $"&state={Uri.EscapeDataString(state)}"
            + $"&response_mode=form_post";

        return authUrl;
    }

    private string BuildOidcAuthUrl(string redirectUri, string state, string codeChallenge)
    {
        var scopes = "openid profile email";
        if (_settings.OpenIdConnect.Scopes.Length > 0)
        {
            scopes = string.Join(" ", _settings.OpenIdConnect.Scopes);
        }

        var authUrl = $"{_settings.OpenIdConnect.Authority.TrimEnd('/')}/authorize"
            + $"?client_id={Uri.EscapeDataString(_settings.OpenIdConnect.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&response_type=code"
            + $"&scope={Uri.EscapeDataString(scopes)}"
            + $"&state={Uri.EscapeDataString(state)}"
            + $"&code_challenge={Uri.EscapeDataString(codeChallenge)}"
            + $"&code_challenge_method=S256";

        return authUrl;
    }

    private async Task<ExternalUserInfo> ExchangeGoogleCodeAsync(
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        // Exchange code for tokens
        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _settings.Google.ClientId,
            ["client_secret"] = _settings.Google.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };

        var tokenResponse = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Google token exchange failed: {Response}", tokenContent);
            throw new InvalidCredentialsException("Failed to exchange authorization code");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        var idToken = tokenData.GetProperty("id_token").GetString()!;

        // Parse ID token to get user info
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        return new ExternalUserInfo
        {
            ProviderId = jwt.Claims.First(c => c.Type == "sub").Value,
            Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
            Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
            GivenName = jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
            FamilyName = jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value
        };
    }

    private async Task<ExternalUserInfo> ExchangeAppleCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        // Generate client secret (JWT signed with Apple private key)
        var clientSecret = GenerateAppleClientSecret();

        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _settings.Apple.ClientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        };

        var tokenResponse = await client.PostAsync(
            "https://appleid.apple.com/auth/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Apple token exchange failed: {Response}", tokenContent);
            throw new InvalidCredentialsException("Failed to exchange authorization code");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        var idToken = tokenData.GetProperty("id_token").GetString()!;

        // Parse ID token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        return new ExternalUserInfo
        {
            ProviderId = jwt.Claims.First(c => c.Type == "sub").Value,
            Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
            // Apple provides name only on first authentication
            Name = null,
            GivenName = null,
            FamilyName = null
        };
    }

    private async Task<ExternalUserInfo> ExchangeOidcCodeAsync(
        string code,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _settings.OpenIdConnect.ClientId,
            ["client_secret"] = _settings.OpenIdConnect.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };

        var tokenEndpoint = $"{_settings.OpenIdConnect.Authority.TrimEnd('/')}/token";
        var tokenResponse = await client.PostAsync(
            tokenEndpoint,
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("OIDC token exchange failed: {Response}", tokenContent);
            throw new InvalidCredentialsException("Failed to exchange authorization code");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
        var idToken = tokenData.GetProperty("id_token").GetString()!;

        // Parse ID token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(idToken);

        return new ExternalUserInfo
        {
            ProviderId = jwt.Claims.First(c => c.Type == "sub").Value,
            Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
            Name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
            GivenName = jwt.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
            FamilyName = jwt.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value
        };
    }

    private string GenerateAppleClientSecret()
    {
        // Load the private key
        var privateKey = _settings.Apple.PrivateKey;

        // Create the key
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKey);

        var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = _settings.Apple.KeyId };
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);

        var now = DateTime.UtcNow;
        var handler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Apple.TeamId,
            Audience = "https://appleid.apple.com",
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("sub", _settings.Apple.ClientId)
            }),
            NotBefore = now,
            Expires = now.AddMinutes(5),
            SigningCredentials = signingCredentials
        };

        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private async Task<User> FindOrCreateUserAsync(
        string provider,
        ExternalUserInfo userInfo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            throw new InvalidCredentialsException("Email is required for authentication");
        }

        var email = userInfo.Email.ToLower().Trim();

        // Check if this external login already exists
        var existingExternalLogin = await _context.UserExternalLogins
            .Include(uel => uel.User)
            .FirstOrDefaultAsync(uel =>
                uel.Provider == provider &&
                uel.ProviderUserId == userInfo.ProviderId,
                cancellationToken);

        if (existingExternalLogin != null)
        {
            // Update last used
            existingExternalLogin.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in via {Provider}", existingExternalLogin.UserId, provider);
            return existingExternalLogin.User;
        }

        // Check if user exists with this email
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Get tenant ID
        var tenantIdString = _configuration["SelfHosted:TenantId"]
            ?? "00000000-0000-0000-0000-000000000001";
        var tenantId = Guid.Parse(tenantIdString);

        if (existingUser != null)
        {
            // Link external login to existing user
            var externalLogin = new UserExternalLogin
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = existingUser.Id,
                Provider = provider,
                ProviderUserId = userInfo.ProviderId,
                ProviderDisplayName = userInfo.Name,
                ProviderEmail = userInfo.Email,
                LastUsedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserExternalLogins.Add(externalLogin);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Linked {Provider} to existing user {UserId}", provider, existingUser.Id);
            return existingUser;
        }

        // Create new user
        var firstName = userInfo.GivenName ?? userInfo.Name?.Split(' ').FirstOrDefault() ?? "User";
        var lastName = userInfo.FamilyName ?? userInfo.Name?.Split(' ').LastOrDefault() ?? "";

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Username = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = string.Empty, // No password for external auth users
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Check if this is the first user (should be Admin)
        var isFirstUser = !await _context.Users.AnyAsync(u => u.Id != user.Id, cancellationToken);
        var role = isFirstUser ? Role.Admin : Role.Viewer;

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserRoles.Add(userRole);

        // Create external login record
        var newExternalLogin = new UserExternalLogin
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            Provider = provider,
            ProviderUserId = userInfo.ProviderId,
            ProviderDisplayName = userInfo.Name,
            ProviderEmail = userInfo.Email,
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserExternalLogins.Add(newExternalLogin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user {UserId} via {Provider} with role {Role}", user.Id, provider, role);

        // Create contact for the user
        try
        {
            await _contactService.CreateContactForUserAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create contact for user {UserId}", user.Id);
        }

        return user;
    }

    private async Task<LoginResponse> GenerateLoginResponseAsync(
        User user,
        string ipAddress,
        string deviceInfo,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        // Load user with permissions and roles
        var loadedUser = await _context.Users
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .Include(u => u.UserRoles)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        // Check user is active
        if (!loadedUser.IsActive)
        {
            throw new AccountInactiveException();
        }

        // Get permissions and roles
        var permissions = loadedUser.UserPermissions.Select(up => up.Permission.Name).ToList();
        var roles = loadedUser.UserRoles.Select(ur => ur.Role).ToList();

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(loadedUser, permissions, roles);
        var accessTokenExpiration = _tokenService.GetTokenExpiration();
        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshTokenString);

        var defaultExpirationDays = _configuration.GetValue("JwtSettings:RefreshTokenExpirationDays", 7);
        var extendedExpirationDays = _configuration.GetValue("JwtSettings:RefreshTokenExtendedExpirationDays", 30);
        var refreshTokenExpirationDays = rememberMe ? extendedExpirationDays : defaultExpirationDays;

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = loadedUser.Id,
            TenantId = loadedUser.TenantId,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            DeviceInfo = deviceInfo ?? string.Empty,
            IpAddress = ipAddress ?? string.Empty,
            RememberMe = rememberMe,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);

        loadedUser.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = _mapper.Map<UserDto>(loadedUser);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = accessTokenExpiration,
            User = userDto
        };
    }

    private static string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private string GetProviderDisplayName(string provider)
    {
        return provider.ToUpperInvariant() switch
        {
            "GOOGLE" => "Google",
            "APPLE" => "Apple",
            "OIDC" => _settings.OpenIdConnect.DisplayName,
            _ => provider
        };
    }

    #endregion

    #region Helper Classes

    private class OAuthStateData
    {
        public string Provider { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public Guid? LinkUserId { get; set; }
        public bool IsLinkOperation { get; set; }
    }

    private class ExternalUserInfo
    {
        public string ProviderId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
    }

    #endregion
}
