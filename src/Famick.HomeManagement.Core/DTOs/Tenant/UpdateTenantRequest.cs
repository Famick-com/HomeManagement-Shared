using Famick.HomeManagement.Core.DTOs.Common;

namespace Famick.HomeManagement.Core.DTOs.Tenant;

public class UpdateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public UpdateAddressRequest? Address { get; set; }
}
