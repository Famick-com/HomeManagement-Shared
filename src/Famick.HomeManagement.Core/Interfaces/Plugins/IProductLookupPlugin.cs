using System.Text.Json;

namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Interface for product lookup plugins that search external databases
/// (USDA FoodData Central, Open Food Facts, etc.)
/// </summary>
public interface IProductLookupPlugin
{
    /// <summary>
    /// Unique identifier for this plugin (e.g., "usda", "openfoodfacts")
    /// Also used as the key in plugins/config.json
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// Human-readable display name (e.g., "USDA FoodData Central")
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Plugin version string
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Priority for ordering plugins (lower = higher priority)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Whether the plugin is currently available (initialized and configured)
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Initialize the plugin with its configuration section from plugins/config.json
    /// Each plugin defines its own configuration schema
    /// </summary>
    /// <param name="pluginConfig">The plugin's configuration section as a JsonElement, or null if not configured</param>
    /// <param name="ct">Cancellation token</param>
    Task InitAsync(JsonElement? pluginConfig, CancellationToken ct = default);

    /// <summary>
    /// Search for products by barcode (UPC, EAN, etc.)
    /// </summary>
    /// <param name="barcode">The barcode to search for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching products</returns>
    Task<List<ProductLookupResult>> SearchByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>
    /// Search for products by name/description
    /// </summary>
    /// <param name="query">Search query string</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching products</returns>
    Task<List<ProductLookupResult>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken ct = default);
}

/// <summary>
/// Result from a product lookup search
/// </summary>
public class ProductLookupResult
{
    /// <summary>
    /// External ID from the source (e.g., fdcId from USDA)
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Plugin ID that provided this result
    /// </summary>
    public string DataSource { get; set; } = string.Empty;

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
}

/// <summary>
/// Nutrition information from a product lookup
/// </summary>
public class ProductLookupNutrition
{
    public decimal? ServingSize { get; set; }
    public string? ServingUnit { get; set; }

    // Macronutrients
    public decimal? Calories { get; set; }
    public decimal? TotalFat { get; set; }
    public decimal? SaturatedFat { get; set; }
    public decimal? TransFat { get; set; }
    public decimal? Cholesterol { get; set; }
    public decimal? Sodium { get; set; }
    public decimal? TotalCarbohydrates { get; set; }
    public decimal? DietaryFiber { get; set; }
    public decimal? TotalSugars { get; set; }
    public decimal? AddedSugars { get; set; }
    public decimal? Protein { get; set; }

    // Vitamins
    public decimal? VitaminA { get; set; }
    public decimal? VitaminC { get; set; }
    public decimal? VitaminD { get; set; }
    public decimal? VitaminE { get; set; }
    public decimal? VitaminK { get; set; }
    public decimal? Thiamin { get; set; }
    public decimal? Riboflavin { get; set; }
    public decimal? Niacin { get; set; }
    public decimal? VitaminB6 { get; set; }
    public decimal? Folate { get; set; }
    public decimal? VitaminB12 { get; set; }

    // Minerals
    public decimal? Calcium { get; set; }
    public decimal? Iron { get; set; }
    public decimal? Magnesium { get; set; }
    public decimal? Phosphorus { get; set; }
    public decimal? Potassium { get; set; }
    public decimal? Zinc { get; set; }
}
