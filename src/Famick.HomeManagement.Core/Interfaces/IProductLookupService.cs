using Famick.HomeManagement.Core.Interfaces.Plugins;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for searching products across all available plugins
/// </summary>
public interface IProductLookupService
{
    /// <summary>
    /// Search for products across all enabled plugins.
    /// Automatically detects if the query is a barcode (8-14 digits) or a name search.
    /// </summary>
    /// <param name="query">Search query (barcode or product name)</param>
    /// <param name="maxResults">Maximum results per plugin</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Combined results from all plugins</returns>
    Task<List<ProductLookupResult>> SearchAsync(string query, int maxResults = 20, CancellationToken ct = default);

    /// <summary>
    /// Search for products by barcode across all enabled plugins
    /// </summary>
    Task<List<ProductLookupResult>> SearchByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>
    /// Search for products by name across all enabled plugins
    /// </summary>
    Task<List<ProductLookupResult>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken ct = default);

    /// <summary>
    /// Apply a lookup result to an existing product, updating its nutrition data
    /// </summary>
    /// <param name="productId">The product ID to update</param>
    /// <param name="result">The lookup result to apply</param>
    /// <param name="ct">Cancellation token</param>
    Task ApplyLookupResultAsync(Guid productId, ProductLookupResult result, CancellationToken ct = default);

    /// <summary>
    /// Get available plugins for display in the UI
    /// </summary>
    IReadOnlyList<PluginInfo> GetAvailablePlugins();
}

/// <summary>
/// Plugin information for display
/// </summary>
public class PluginInfo
{
    public string PluginId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
