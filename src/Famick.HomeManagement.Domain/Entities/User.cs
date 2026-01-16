using Famick.HomeManagement.Domain.Interfaces;

namespace Famick.HomeManagement.Domain.Entities;

/// <summary>
/// Represents a user within a tenant
/// </summary>
public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// User's preferred language code (e.g., "en", "es", "fr")
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Link to the user's Contact record (1:1 relationship)
    /// </summary>
    public Guid? ContactId { get; set; }

    // Navigation properties
    // Note: Tenant navigation property is cloud-specific and defined in homemanagement-cloud
    public virtual Contact? Contact { get; set; }
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
