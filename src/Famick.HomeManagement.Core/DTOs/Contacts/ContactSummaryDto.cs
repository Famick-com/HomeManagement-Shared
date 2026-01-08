using Famick.HomeManagement.Domain.Enums;

namespace Famick.HomeManagement.Core.DTOs.Contacts;

/// <summary>
/// Summary view of a contact for list displays
/// </summary>
public class ContactSummaryDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Display name for the contact
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(PreferredName)
        ? PreferredName
        : $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Primary phone number if available
    /// </summary>
    public string? PrimaryPhone { get; set; }

    /// <summary>
    /// Primary address display if available
    /// </summary>
    public string? PrimaryAddress { get; set; }

    /// <summary>
    /// Whether this contact is linked to a system user
    /// </summary>
    public bool IsUserLinked { get; set; }

    /// <summary>
    /// Tag names for display
    /// </summary>
    public List<string> TagNames { get; set; } = new();

    /// <summary>
    /// Tag colors for display
    /// </summary>
    public List<string?> TagColors { get; set; } = new();

    public ContactVisibilityLevel Visibility { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
