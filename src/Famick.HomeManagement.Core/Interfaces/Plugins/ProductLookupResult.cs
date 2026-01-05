namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Result from a product lookup search
/// </summary>
public class ProductLookupResult
{
    /// <summary>
    /// Data sources that contributed to this result.
    /// Key = Plugin DisplayName, Value = External ID from that source
    /// </summary>
    public Dictionary<string, string> DataSources { get; set; } = new();

    /// <summary>
    /// Product name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brand name (if available)
    /// </summary>
    public string? BrandName { get; set; }

    /// <summary>
    /// Brand owner/manufacturer (if available)
    /// </summary>
    public string? BrandOwner { get; set; }

    /// <summary>
    /// Barcode (UPC/EAN) if available
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Food category (e.g., "Cheese", "Snacks", "Beverages")
    /// </summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>
    /// Product image (if available). First plugin to provide wins.
    /// </summary>
    public ResultImage? ImageUrl { get; set; }

    /// <summary>
    /// URL to product thumbnail image (if available)
    /// </summary>
    public ResultImage? ThumbnailUrl { get; set; }

    /// <summary>
    /// Serving size description
    /// </summary>
    public string? ServingSizeDescription { get; set; }

    /// <summary>
    /// Ingredients list (if available)
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>
    /// Nutrition information (if available)
    /// </summary>
    public ProductLookupNutrition? Nutrition { get; set; }

    /// <summary>
    /// Additional data from the source (plugin-specific)
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }
    
    /// <summary>
    /// Description of the product
    /// </summary>
    public string? Description { get; set; }
}
