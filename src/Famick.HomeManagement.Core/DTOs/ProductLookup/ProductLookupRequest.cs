namespace Famick.HomeManagement.Core.DTOs.ProductLookup;

/// <summary>
/// Request for product lookup search
/// </summary>
public class ProductLookupRequest
{
    /// <summary>
    /// Search query - can be a barcode (8-14 digits) or a product name.
    /// The system auto-detects which type of search to perform.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return (default: 20)
    /// </summary>
    public int MaxResults { get; set; } = 20;

    /// <summary>
    /// Optional: Specific plugin ID to search (if null, searches all enabled plugins)
    /// </summary>
    public string? PluginId { get; set; }
}
