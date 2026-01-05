using Famick.HomeManagement.Core.Interfaces.Plugins;

namespace Famick.HomeManagement.Core.DTOs.ProductLookup;

/// <summary>
/// Result from product lookup that can come from either
/// product plugins (USDA, Open Food Facts) or store integrations (Kroger)
/// </summary>
public class ProductLookupResultDto
{
    /// <summary>
    /// Source type: "ProductPlugin" or "StoreIntegration"
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// Plugin ID that provided this result (e.g., "usda", "kroger")
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the source (e.g., "USDA FoodData Central", "Kroger")
    /// </summary>
    public string PluginDisplayName { get; set; } = string.Empty;

    // ===== Common product fields =====

    /// <summary>
    /// External ID from the source (fdcId from USDA, product ID from Kroger, etc.)
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Product name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brand name
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Barcode (UPC/EAN)
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Food/product category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// URL to product image
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// URL to product thumbnail image
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    // ===== Product plugin-specific fields (null for store results) =====

    /// <summary>
    /// Nutrition information (from product plugins only)
    /// </summary>
    public ProductLookupNutrition? Nutrition { get; set; }

    /// <summary>
    /// Ingredients list
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>
    /// Serving size description
    /// </summary>
    public string? ServingSizeDescription { get; set; }

    /// <summary>
    /// Brand owner/manufacturer
    /// </summary>
    public string? BrandOwner { get; set; }

    // ===== Store integration-specific fields (null for product plugin results) =====

    /// <summary>
    /// Current price at the store
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Price unit (e.g., "each", "lb")
    /// </summary>
    public string? PriceUnit { get; set; }

    /// <summary>
    /// Sale/promotional price
    /// </summary>
    public decimal? SalePrice { get; set; }

    /// <summary>
    /// Aisle location in the store
    /// </summary>
    public string? Aisle { get; set; }

    /// <summary>
    /// Shelf location in the store
    /// </summary>
    public string? Shelf { get; set; }

    /// <summary>
    /// Department or category in the store
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Whether the product is currently in stock
    /// </summary>
    public bool? InStock { get; set; }

    /// <summary>
    /// Product size/weight description
    /// </summary>
    public string? Size { get; set; }

    /// <summary>
    /// URL to the product page on the store's website
    /// </summary>
    public string? ProductUrl { get; set; }

    // ===== Store context (for store results only) =====

    /// <summary>
    /// Shopping location ID where this product was found
    /// </summary>
    public Guid? ShoppingLocationId { get; set; }

    /// <summary>
    /// Shopping location name where this product was found
    /// </summary>
    public string? ShoppingLocationName { get; set; }
}
