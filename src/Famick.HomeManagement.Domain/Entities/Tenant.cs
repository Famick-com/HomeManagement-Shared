namespace Famick.HomeManagement.Domain.Entities;

/// <summary>
/// Represents a tenant (household) in the system.
/// In self-hosted mode, there is exactly one tenant with the fixed ID.
/// In cloud SaaS mode, multiple tenants can exist.
/// The Tenant's Id IS the TenantId used throughout BaseTenantEntity.
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// Display name for the household (e.g., "The Smiths", "Beach House")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional FK to the tenant's primary address
    /// </summary>
    public Guid? AddressId { get; set; }

    /// <summary>
    /// Navigation property to the tenant's address
    /// </summary>
    public virtual Address? Address { get; set; }
}
