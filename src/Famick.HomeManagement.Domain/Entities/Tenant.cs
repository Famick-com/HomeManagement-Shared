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
    /// Subdomain for cloud multi-tenant routing (e.g., "acme" for acme.famick.com).
    /// Not used in self-hosted mode.
    /// </summary>
    public string? Subdomain { get; set; }

    /// <summary>
    /// Whether the tenant account is active.
    /// Inactive tenants cannot access the system. Defaults to true.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// AWS KMS key ID used for per-tenant encryption of stored files.
    /// Only used in cloud deployments; null in self-hosted mode.
    /// </summary>
    public string? KmsKeyId { get; set; }

    /// <summary>
    /// Optional FK to the tenant's primary address
    /// </summary>
    public Guid? AddressId { get; set; }

    /// <summary>
    /// Navigation property to the tenant's address
    /// </summary>
    public virtual Address? Address { get; set; }
}
