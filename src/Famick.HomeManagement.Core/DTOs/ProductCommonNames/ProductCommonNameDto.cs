namespace Famick.HomeManagement.Core.DTOs.ProductCommonNames;

/// <summary>
/// DTO for ProductCommonName entity
/// </summary>
public class ProductCommonNameDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
