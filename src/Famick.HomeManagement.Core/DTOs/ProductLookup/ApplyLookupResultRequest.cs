namespace Famick.HomeManagement.Core.DTOs.ProductLookup;

/// <summary>
/// Request to apply a lookup result to a product
/// </summary>
public class ApplyLookupResultRequest
{
    /// <summary>
    /// External ID from the lookup result
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Plugin ID that provided the result
    /// </summary>
    public string DataSource { get; set; } = string.Empty;

    /// <summary>
    /// Product name from the lookup
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Brand name from the lookup
    /// </summary>
    public string? BrandName { get; set; }

    /// <summary>
    /// Brand owner from the lookup
    /// </summary>
    public string? BrandOwner { get; set; }

    /// <summary>
    /// Barcode from the lookup
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Serving size description
    /// </summary>
    public string? ServingSizeDescription { get; set; }

    /// <summary>
    /// Ingredients list
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>
    /// Whether to update the product name
    /// </summary>
    public bool UpdateName { get; set; } = true;

    /// <summary>
    /// Whether to add the barcode to the product
    /// </summary>
    public bool AddBarcode { get; set; } = true;

    /// <summary>
    /// Nutrition data to apply
    /// </summary>
    public ProductNutritionDto? Nutrition { get; set; }
}
