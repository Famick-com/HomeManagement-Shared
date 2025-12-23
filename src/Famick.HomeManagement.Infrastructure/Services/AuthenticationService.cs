using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Authentication;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Domain.Enums;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// Implementation of authentication and authorization services
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly HomeManagementDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        HomeManagementDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IMapper mapper,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _mapper = mapper;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RegisterTenantResponse> RegisterTenantAsync(
        RegisterTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check subdomain uniqueness
        var subdomainExists = await _context.Tenants
            .AnyAsync(t => t.Subdomain == request.Subdomain.ToLower(), cancellationToken);

        if (subdomainExists)
        {
            throw new InvalidOperationException($"Subdomain '{request.Subdomain}' is already taken");
        }

        // Check email uniqueness across all tenants
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email.ToLower(), cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered");
        }

        // Use execution strategy to handle transactions with retry logic
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async (ct) =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                // 1. Create tenant
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = request.OrganizationName,
                    Subdomain = request.Subdomain.ToLower(),
                    SubscriptionTier = SubscriptionTier.Free,
                    MaxUsers = 5,
                    StorageQuotaMb = 1000,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync(ct);

                // 2. Create admin user
                var hashedPassword = _passwordHasher.HashPassword(request.Password);

                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Username = request.Email.ToLower(),
                    Email = request.Email.ToLower(),
                    PasswordHash = hashedPassword,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync(ct);

                // 3. Assign default admin permissions
                var defaultPermissions = _configuration.GetSection("DefaultAdminPermissions").Get<List<string>>()
                    ?? new List<string>
                    {
                        "stock.purchase", "stock.consume", "stock.transfer", "stock.inventory",
                        "recipes.create", "recipes.edit", "recipes.delete", "recipes.view",
                        "shopping_list.create", "shopping_list.edit", "shopping_list.delete",
                        "chores.create", "chores.edit", "chores.execute",
                        "tasks.create", "tasks.edit", "tasks.complete",
                        "users.manage", "settings.manage"
                    };

                // Get or create permissions
                foreach (var permissionName in defaultPermissions)
                {
                    var permission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.Name == permissionName, ct);

                    if (permission == null)
                    {
                        permission = new Permission
                        {
                            Id = Guid.NewGuid(),
                            Name = permissionName,
                            Description = $"Permission for {permissionName}",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Permissions.Add(permission);
                        await _context.SaveChangesAsync(ct);
                    }

                    // Assign permission to user
                    var userPermission = new UserPermission
                    {
                        UserId = adminUser.Id,
                        PermissionId = permission.Id,
                        TenantId = tenant.Id
                    };

                    _context.UserPermissions.Add(userPermission);
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("New tenant registered: {Subdomain}, Admin user: {Email}",
                    tenant.Subdomain, adminUser.Email);

                return new RegisterTenantResponse
                {
                    UserId = adminUser.Id,
                    TenantId = tenant.Id,
                    Subdomain = tenant.Subdomain,
                    Message = $"Registration successful. Please login at https://{tenant.Subdomain}.famick.com"
                };
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string deviceInfo,
        CancellationToken cancellationToken = default)
    {
        // Find user by email (case-insensitive)
        var user = await _context.Users
            .Include(u => u.Tenant)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower(), cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            throw new InvalidCredentialsException();
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            throw new InvalidCredentialsException();
        }

        // Check user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
            throw new AccountInactiveException();
        }

        // Check tenant is active
        if (!user.Tenant.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive tenant: {TenantId}", user.TenantId);
            throw new TenantInactiveException();
        }

        // Get user permissions
        var permissions = user.UserPermissions
            .Select(up => up.Permission.Name)
            .ToList();

        // Generate access token
        var accessToken = _tokenService.GenerateAccessToken(user, permissions);
        var accessTokenExpiration = _tokenService.GetTokenExpiration();

        // Generate refresh token
        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshTokenString);

        var refreshTokenExpirationDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            DeviceInfo = deviceInfo ?? string.Empty,
            IpAddress = ipAddress ?? string.Empty,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in: {Email}, IP: {IpAddress}", user.Email, ipAddress);

        // Map to DTOs
        var userDto = _mapper.Map<UserDto>(user);
        var tenantDto = _mapper.Map<TenantDto>(user.Tenant);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = accessTokenExpiration,
            User = userDto,
            Tenant = tenantDto
        };
    }

    /// <inheritdoc />
    public async Task<RefreshTokenResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string ipAddress,
        string deviceInfo,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);

        // Find refresh token
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Tenant)
            .Include(rt => rt.User.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            _logger.LogWarning("Refresh token not found or inactive. IP: {IpAddress}", ipAddress);
            throw new InvalidCredentialsException("Invalid or expired refresh token");
        }

        // Check user and tenant are still active
        if (!refreshToken.User.IsActive)
        {
            throw new AccountInactiveException();
        }

        if (!refreshToken.User.Tenant.IsActive)
        {
            throw new TenantInactiveException();
        }

        // Get user permissions
        var permissions = refreshToken.User.UserPermissions
            .Select(up => up.Permission.Name)
            .ToList();

        // Generate new access token
        var newAccessToken = _tokenService.GenerateAccessToken(refreshToken.User, permissions);
        var newAccessTokenExpiration = _tokenService.GetTokenExpiration();

        // Generate new refresh token (rotation)
        var newRefreshTokenString = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = HashToken(newRefreshTokenString);

        var refreshTokenExpirationDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = refreshToken.UserId,
            TenantId = refreshToken.TenantId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            DeviceInfo = deviceInfo ?? string.Empty,
            IpAddress = ipAddress ?? string.Empty,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshToken);

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByTokenId = newRefreshToken.Id;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refreshed for user: {UserId}, IP: {IpAddress}",
            refreshToken.UserId, ipAddress);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = newAccessTokenExpiration
        };
    }

    /// <inheritdoc />
    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token != null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token revoked for user: {UserId}", token.UserId);
        }
    }

    /// <inheritdoc />
    public async Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        if (activeTokens.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("All refresh tokens revoked for user: {UserId}, Count: {Count}",
                userId, activeTokens.Count);
        }
    }

    /// <summary>
    /// Hashes a token using SHA256 for secure storage
    /// </summary>
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
