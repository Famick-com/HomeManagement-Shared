namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Provides access to the current tenant context
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Gets the current tenant ID, or null if no tenant is set
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Sets the current tenant ID
    /// </summary>
    void SetTenantId(Guid tenantId);

    /// <summary>
    /// Clears the current tenant ID
    /// </summary>
    void ClearTenantId();
}
