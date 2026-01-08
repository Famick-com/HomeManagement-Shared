using Famick.HomeManagement.Domain.Enums;

namespace Famick.HomeManagement.Domain.Entities;

/// <summary>
/// Represents a contact in the system. Contacts can be linked to users,
/// shared across the tenant, or kept private.
/// </summary>
public class Contact : BaseTenantEntity
{
    // Name
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? Email { get; set; }

    // Demographics
    public Gender Gender { get; set; } = Gender.Unknown;

    // Birth Date (nullable components for partial date support)
    public int? BirthYear { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthDay { get; set; }
    public DatePrecision BirthDatePrecision { get; set; } = DatePrecision.Unknown;

    // Death Date (nullable components for partial date support)
    public int? DeathYear { get; set; }
    public int? DeathMonth { get; set; }
    public int? DeathDay { get; set; }
    public DatePrecision DeathDatePrecision { get; set; } = DatePrecision.Unknown;

    public string? Notes { get; set; }

    /// <summary>
    /// Reference to another tenant if this contact belongs to a different household
    /// </summary>
    public Guid? HouseholdTenantId { get; set; }

    /// <summary>
    /// Link to User entity if this contact represents a system user
    /// </summary>
    public Guid? LinkedUserId { get; set; }

    /// <summary>
    /// When true, this contact's home address references the tenant's address
    /// </summary>
    public bool UsesTenantAddress { get; set; } = false;

    /// <summary>
    /// User who created this contact
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Visibility level for this contact
    /// </summary>
    public ContactVisibilityLevel Visibility { get; set; } = ContactVisibilityLevel.TenantShared;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual User? LinkedUser { get; set; }
    public virtual User CreatedByUser { get; set; } = null!;
    public virtual ICollection<ContactAddress> Addresses { get; set; } = new List<ContactAddress>();
    public virtual ICollection<ContactPhoneNumber> PhoneNumbers { get; set; } = new List<ContactPhoneNumber>();
    public virtual ICollection<ContactSocialMedia> SocialMedia { get; set; } = new List<ContactSocialMedia>();
    public virtual ICollection<ContactRelationship> RelationshipsAsSource { get; set; } = new List<ContactRelationship>();
    public virtual ICollection<ContactRelationship> RelationshipsAsTarget { get; set; } = new List<ContactRelationship>();
    public virtual ICollection<ContactTagLink> Tags { get; set; } = new List<ContactTagLink>();
    public virtual ICollection<ContactUserShare> SharedWithUsers { get; set; } = new List<ContactUserShare>();
    public virtual ICollection<ContactAuditLog> AuditLogs { get; set; } = new List<ContactAuditLog>();

    /// <summary>
    /// Gets the display name for this contact
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(PreferredName)
        ? PreferredName
        : $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets the full name for this contact
    /// </summary>
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}".Trim()
        : $"{FirstName} {MiddleName} {LastName}".Trim();
}
