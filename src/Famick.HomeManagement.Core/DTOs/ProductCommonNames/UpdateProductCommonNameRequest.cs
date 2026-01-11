namespace Famick.HomeManagement.Core.DTOs.ProductCommonNames;

/// <summary>
/// Request to update an existing product common name
/// </summary>
public class UpdateProductCommonNameRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
