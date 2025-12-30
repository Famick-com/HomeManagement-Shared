namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Result from an OAuth token exchange or refresh operation
/// </summary>
public class OAuthTokenResult
{
    /// <summary>
    /// Whether the token operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The access token for API calls
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The refresh token for obtaining new access tokens
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When the access token expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful token result
    /// </summary>
    public static OAuthTokenResult Ok(string accessToken, string? refreshToken, DateTime expiresAt) => new()
    {
        Success = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        ExpiresAt = expiresAt
    };

    /// <summary>
    /// Creates a failed token result
    /// </summary>
    public static OAuthTokenResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result from a store location search
/// </summary>
public class StoreLocationResult
{
    /// <summary>
    /// External store location ID from the integration provider
    /// </summary>
    public string ExternalLocationId { get; set; } = string.Empty;

    /// <summary>
    /// Store name (e.g., "Kroger #12345")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Chain/brand identifier (e.g., "kroger", "ralphs", "fred-meyer")
    /// </summary>
    public string? ChainId { get; set; }

    /// <summary>
    /// Chain/brand display name (e.g., "Kroger", "Ralphs", "Fred Meyer")
    /// </summary>
    public string? ChainName { get; set; }

    /// <summary>
    /// Full street address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State/province code
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// ZIP/postal code
    /// </summary>
    public string? ZipCode { get; set; }

    /// <summary>
    /// Store phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Store latitude
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Store longitude
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Distance from search location in miles
    /// </summary>
    public double? DistanceMiles { get; set; }

    /// <summary>
    /// Formats the full address as a single line
    /// </summary>
    public string? FullAddress => !string.IsNullOrEmpty(Address)
        ? $"{Address}, {City}, {State} {ZipCode}"
        : null;
}

/// <summary>
/// Result from a store product search or lookup
/// </summary>
public class StoreProductResult
{
    /// <summary>
    /// Store's internal product ID/SKU
    /// </summary>
    public string ExternalProductId { get; set; } = string.Empty;

    /// <summary>
    /// Product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Brand name
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Product barcode (UPC/EAN)
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// URL to product image
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Current price
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Price unit (e.g., "each", "lb", "oz")
    /// </summary>
    public string? PriceUnit { get; set; }

    /// <summary>
    /// Sale/promotional price (if on sale)
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
    /// Department or category
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
}
