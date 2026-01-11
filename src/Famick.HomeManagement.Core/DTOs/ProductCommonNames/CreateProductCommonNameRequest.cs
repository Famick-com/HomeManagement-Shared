namespace Famick.HomeManagement.Core.DTOs.ProductCommonNames;

/// <summary>
/// Request to create a new product common name
/// </summary>
public class CreateProductCommonNameRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
