using Famick.HomeManagement.Core.Interfaces;

namespace Famick.HomeManagement.Core.Services;

/// <summary>
/// Provides access to the current tenant context using AsyncLocal for thread-safe storage
/// </summary>
public class TenantProvider : ITenantProvider
{
    private static readonly AsyncLocal<Guid?> _tenantId = new();

    public Guid? TenantId => _tenantId.Value;

    public void SetTenantId(Guid tenantId)
    {
        _tenantId.Value = tenantId;
    }

    public void ClearTenantId()
    {
        _tenantId.Value = null;
    }
}
