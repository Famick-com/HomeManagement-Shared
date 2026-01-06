using System.Security.Cryptography;
using Famick.HomeManagement.Core.DTOs.Users;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Domain.Enums;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// Service for managing users (Admin only)
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly HomeManagementDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UserManagementService> _logger;

    private const int GeneratedPasswordLength = 12;
    private const string PasswordChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%";

    public UserManagementService(
        HomeManagementDbContext context,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ITenantProvider tenantProvider,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ManagedUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        return users.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ManagedUserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user == null ? null : MapToDto(user);
    }

    /// <inheritdoc />
    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.ToLower().Trim();

        // Check for duplicate email
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser != null)
        {
            throw new DuplicateEntityException("User", "Email", email);
        }

        // Generate password if not provided
        string password;
        string? generatedPassword = null;
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            password = GenerateRandomPassword();
            generatedPassword = password; // Return to admin
        }
        else
        {
            password = request.Password;
        }

        var tenantId = _tenantProvider.TenantId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Username = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Assign roles
        foreach (var role in request.Roles)
        {
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
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created: {Email}, ID: {UserId}", email, user.Id);

        // Send welcome email if requested
        bool welcomeEmailSent = false;
        if (request.SendWelcomeEmail)
        {
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    email,
                    $"{request.FirstName} {request.LastName}".Trim(),
                    password,
                    cancellationToken);
                welcomeEmailSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", email);
            }
        }

        return new CreateUserResponse
        {
            UserId = user.Id,
            Email = email,
            GeneratedPassword = generatedPassword,
            WelcomeEmailSent = welcomeEmailSent
        };
    }

    /// <inheritdoc />
    public async Task<ManagedUserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        var newEmail = request.Email.ToLower().Trim();

        // Check for duplicate email if changing
        if (user.Email != newEmail)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == newEmail && u.Id != userId, cancellationToken);

            if (existingUser != null)
            {
                throw new DuplicateEntityException("User", "Email", newEmail);
            }

            user.Email = newEmail;
            user.Username = newEmail;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Update roles - remove old and add new
        var existingRoles = user.UserRoles.ToList();
        _context.UserRoles.RemoveRange(existingRoles);

        foreach (var role in request.Roles)
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = user.Id,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User updated: {Email}, ID: {UserId}", user.Email, user.Id);

        // Reload to get updated roles
        user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstAsync(u => u.Id == userId, cancellationToken);

        return MapToDto(user);
    }

    /// <inheritdoc />
    public async Task DeleteUserAsync(Guid userId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (userId == currentUserId)
        {
            throw new BusinessRuleViolationException("CannotDeleteSelf", "You cannot delete your own account");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User deleted: {Email}, ID: {UserId}", user.Email, user.Id);
    }

    /// <inheritdoc />
    public async Task<AdminResetPasswordResponse> AdminResetPasswordAsync(Guid userId, AdminResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new EntityNotFoundException("User", userId);
        }

        // Generate password if not provided
        string password;
        string? generatedPassword = null;
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            password = GenerateRandomPassword();
            generatedPassword = password;
        }
        else
        {
            password = request.NewPassword;
        }

        user.PasswordHash = _passwordHasher.HashPassword(password);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all refresh tokens
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset by admin for user: {Email}, ID: {UserId}", user.Email, user.Id);

        return new AdminResetPasswordResponse
        {
            Success = true,
            GeneratedPassword = generatedPassword
        };
    }

    /// <inheritdoc />
    public async Task<List<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .ToListAsync(cancellationToken);
    }

    private static ManagedUserDto MapToDto(User user)
    {
        return new ManagedUserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            Roles = user.UserRoles.Select(ur => ur.Role).ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    private static string GenerateRandomPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(GeneratedPasswordLength);
        var chars = new char[GeneratedPasswordLength];

        for (int i = 0; i < GeneratedPasswordLength; i++)
        {
            chars[i] = PasswordChars[bytes[i] % PasswordChars.Length];
        }

        return new string(chars);
    }
}
