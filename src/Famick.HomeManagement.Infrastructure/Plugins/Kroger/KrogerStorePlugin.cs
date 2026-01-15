using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Microsoft.Extensions.Logging;
using Serilog.Debugging;

namespace Famick.HomeManagement.Infrastructure.Plugins.Kroger;

/// <summary>
/// Kroger store integration plugin for OAuth, store location, and product lookup
/// </summary>
public class KrogerStorePlugin : IStoreIntegrationPlugin, IProductLookupPlugin
{
    private const string KrogerApiBaseUrl = "https://api.kroger.com/v1";
    private const string KrogerAuthUrl = "https://api.kroger.com/v1/connect/oauth2/authorize";
    private const string KrogerTokenUrl = "https://api.kroger.com/v1/connect/oauth2/token";

    private readonly HttpClient _httpClient;
    private readonly ILogger<KrogerStorePlugin> _logger;

    private string? _clientId;
    private string? _clientSecret;
    private string _scope = "product.compact";

    public string PluginId => "kroger";
    public string DisplayName => "Kroger Family of Stores";
    public string Version => "1.0.0";
    public bool IsAvailable => !string.IsNullOrEmpty(_clientId) && !string.IsNullOrEmpty(_clientSecret);

    public StoreIntegrationCapabilities Capabilities => new()
    {
        RequiresOAuth = true,
        HasProductLookup = true,
        HasStoreProductLookup = true,
        HasShoppingCart = true,
        CanReadShoppingCart = true,
        CanDownloadProductImages = true
    };

    public KrogerStorePlugin(IHttpClientFactory httpClientFactory, ILogger<KrogerStorePlugin> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task InitAsync(JsonElement? pluginConfig, CancellationToken ct = default)
    {
        if (pluginConfig.HasValue)
        {
            var config = pluginConfig.Value;

            if (config.TryGetProperty("clientId", out var clientIdElement))
            {
                _clientId = clientIdElement.GetString();
            }

            if (config.TryGetProperty("clientSecret", out var clientSecretElement))
            {
                _clientSecret = clientSecretElement.GetString();
            }

            if (config.TryGetProperty("scope", out var scopeElement))
            {
                _scope = scopeElement.GetString() ?? "product.compact";
            }
        }

        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            _logger.LogWarning("Kroger plugin not configured: missing clientId or clientSecret");
        }
        else
        {
            _logger.LogInformation("Kroger plugin initialized with scope: {Scope}", _scope);
        }

        return Task.CompletedTask;
    }

    #region OAuth

    public string GetAuthorizationUrl(string redirectUri, string state)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        var queryParams = new Dictionary<string, string>
        {
            { "client_id", _clientId! },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", _scope },
            { "state", state }
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

        return $"{KrogerAuthUrl}?{queryString}";
    }

    public async Task<OAuthTokenResult> ExchangeCodeForTokenAsync(
        string code,
        string redirectUri,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return OAuthTokenResult.Fail("Kroger plugin is not configured");
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, KrogerTokenUrl)
        {
            Content = content
        };

        AddBasicAuthHeader(request);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to exchange code for token. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);
            return OAuthTokenResult.Fail($"Failed to exchange code for token: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<KrogerTokenResponse>(responseContent);
        if (tokenResponse == null)
        {
            return OAuthTokenResult.Fail("Invalid token response from Kroger");
        }

        return OAuthTokenResult.Ok(
            tokenResponse.AccessToken ?? string.Empty,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
    }

    public async Task<OAuthTokenResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            return OAuthTokenResult.Fail("Kroger plugin is not configured");
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, KrogerTokenUrl)
        {
            Content = content
        };

        AddBasicAuthHeader(request);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to refresh token. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);
            return OAuthTokenResult.Fail($"Failed to refresh token: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<KrogerTokenResponse>(responseContent);
        if (tokenResponse == null)
        {
            return OAuthTokenResult.Fail("Invalid token response from Kroger");
        }

        return OAuthTokenResult.Ok(
            tokenResponse.AccessToken ?? string.Empty,
            tokenResponse.RefreshToken ?? refreshToken, // Keep old refresh token if not returned
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
    }

    private void AddBasicAuthHeader(HttpRequestMessage request)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    #endregion

    #region Store Location

    public async Task<List<StoreLocationResult>> SearchStoresByZipAsync(
        string zipCode,
        int radiusMiles = 10,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        // Get a client credentials token for location search (doesn't require user auth)
        var token = await GetClientCredentialsTokenAsync(ct);

        var queryParams = new Dictionary<string, string>
        {
            { "filter.zipCode.near", zipCode },
            { "filter.radiusInMiles", radiusMiles.ToString() },
            { "filter.limit", "20" }
        };

        return await SearchLocationsAsync(token, queryParams, ct);
    }

    public async Task<List<StoreLocationResult>> SearchStoresByCoordinatesAsync(
        double latitude,
        double longitude,
        int radiusMiles = 10,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        var token = await GetClientCredentialsTokenAsync(ct);

        var queryParams = new Dictionary<string, string>
        {
            { "filter.lat.near", latitude.ToString("F6") },
            { "filter.lon.near", longitude.ToString("F6") },
            { "filter.radiusInMiles", radiusMiles.ToString() },
            { "filter.limit", "20" }
        };

        return await SearchLocationsAsync(token, queryParams, ct);
    }

    private async Task<string> GetClientCredentialsTokenAsync(CancellationToken ct, string? scope = null )
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", scope ?? _scope }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, KrogerTokenUrl)
        {
            Content = content
        };

        AddBasicAuthHeader(request);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get client credentials token. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);
            throw new InvalidOperationException($"Failed to get client credentials token: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<KrogerTokenResponse>(responseContent);
        return tokenResponse?.AccessToken ?? throw new InvalidOperationException("Invalid token response");
    }

    private async Task<List<StoreLocationResult>> SearchLocationsAsync(
        string accessToken,
        Dictionary<string, string> queryParams,
        CancellationToken ct)
    {
        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

        var url = $"{KrogerApiBaseUrl}/locations?{queryString}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to search locations. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);
            throw new InvalidOperationException($"Failed to search locations: {response.StatusCode}");
        }

        var locationsResponse = JsonSerializer.Deserialize<KrogerLocationsResponse>(responseContent);
        if (locationsResponse?.Data == null)
        {
            return new List<StoreLocationResult>();
        }

        return locationsResponse.Data.Select(loc => new StoreLocationResult
        {
            ExternalLocationId = loc.LocationId,
            ChainId = loc.Chain,
            ChainName = loc.Chain, // Could be enhanced with a mapping
            Name = loc.Name,
            Address = FormatAddress(loc.Address),
            City = loc.Address?.City,
            State = loc.Address?.State,
            ZipCode = loc.Address?.ZipCode,
            Phone = loc.Phone,
            Latitude = loc.GeoLocation?.Latitude,
            Longitude = loc.GeoLocation?.Longitude
        }).ToList();
    }

    private static string FormatAddress(KrogerAddress? address)
    {
        if (address == null) return string.Empty;
        return $"{address.AddressLine1}, {address.City}, {address.State} {address.ZipCode}";
    }

    #endregion

    #region Product Search

    public async Task<List<StoreProductResult>> SearchProductsAsync(
        string? accessToken,
        string? storeLocationId,
        string query,
        int maxResults = 20,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        var queryParams = new Dictionary<string, string>
        {
            { "filter.limit", maxResults.ToString() }
        };

        if (storeLocationId != null)
        {
            queryParams.Add("filter.locationId", storeLocationId);
        }

        // If query looks like a barcode (all digits), normalize and use productId filter
        if (Regex.IsMatch(query, @"^\d+$"))
        {
            var normalizedBarcode = NormalizeBarcode(query);
            queryParams["filter.productId"] = normalizedBarcode;
        }
        else
        {
            queryParams["filter.term"] = query;
        }

        return await SearchProductsInternalAsync(accessToken, queryParams, ct);
    }

    public async Task<StoreProductResult?> GetProductAsync(
        string accessToken,
        string storeLocationId,
        string productId,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        var queryParams = new Dictionary<string, string>
        {
            { "filter.productId", productId },
            { "filter.locationId", storeLocationId },
            { "filter.limit", "1" }
        };

        var results = await SearchProductsInternalAsync(accessToken, queryParams, ct);
        return results.FirstOrDefault();
    }

    public async Task<StoreProductResult?> LookupProductByBarcodeAsync(
        string? accessToken,
        string storeLocationId,
        string barcode,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        // Use client credentials if no access token provided
        var token = accessToken ?? await GetClientCredentialsTokenAsync(ct);

        var normalizedBarcode = NormalizeBarcode(barcode);

        var queryParams = new Dictionary<string, string>
        {
            { "filter.productId", normalizedBarcode },
            { "filter.locationId", storeLocationId },
            { "filter.limit", "1" }
        };

        var results = await SearchProductsInternalAsync(token, queryParams, ct);
        return results.FirstOrDefault();
    }

    private async Task<List<StoreProductResult>> SearchProductsInternalAsync(
        string? accessToken,
        Dictionary<string, string> queryParams,
        CancellationToken ct)
    {
        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));

        var url = $"{KrogerApiBaseUrl}/products?{queryString}";

        if (accessToken == null)
        {
            accessToken = await this.GetClientCredentialsTokenAsync(scope: "product.compact", ct:ct);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to search products. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);

            // Throw specific exception for auth failures to enable retry with token refresh
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new StoreAuthenticationException(
                    PluginId,
                    $"Authentication failed: {response.StatusCode}",
                    (int)response.StatusCode);
            }

            throw new InvalidOperationException($"Failed to search products: {response.StatusCode}");
        }

        var productsResponse = JsonSerializer.Deserialize<KrogerProductsResponse>(responseContent);
        if (productsResponse?.Data == null)
        {
            return new List<StoreProductResult>();
        }

        return productsResponse.Data.Select(MapToStoreProductResult).ToList();
    }

    private static StoreProductResult MapToStoreProductResult(KrogerProduct product)
    {
        var result = new StoreProductResult
        {
            ExternalProductId = product.ProductId,
            Name = product.Description,
            Brand = product.Brand,
            Barcode = product.Upc,
            Categories = product.Categories ?? new(),
            Description = product.Description,
        };

        

        // Extract image URL
        if (product.Images?.Count > 0)
        {
            var image = product.Images.FirstOrDefault(i => i.Featured) ?? product.Images[0];
            var largeSize = image.Sizes?.FirstOrDefault(s => s.Size == "large")
                ?? image.Sizes?.FirstOrDefault(s => s.Size == "medium")
                ?? image.Sizes?.FirstOrDefault();
            result.ImageUrl = largeSize?.Url;
        }

        // Extract price and availability from first item
        if (product.Items?.Count > 0)
        {
            var item = product.Items[0];
            result.Price = item.Price?.Regular ?? item.Price?.Promo;
            result.SalePrice = item.Price?.Promo;
            result.PriceUnit = item.Size;
            result.Size = item.Size;
            result.InStock = item.Fulfillment?.InStore ?? false;
        }

        // Extract aisle location
        if (product.AisleLocations?.Count > 0)
        {
            var aisleLocation = product.AisleLocations[0];

            // Get aisle - strip "AISLE" prefix if present
            var aisle = aisleLocation.Description ?? aisleLocation.Number ?? aisleLocation.BayNumber;
            if (!string.IsNullOrEmpty(aisle))
            {
                result.Aisle = Regex.Replace(aisle, @"^AISLE\s*", "", RegexOptions.IgnoreCase);
            }

            result.Shelf = aisleLocation.ShelfNumber;
        }

        // Use first category as department
        if (product.Categories?.Count > 0)
        {
            result.Department = product.Categories[0];
        }

        // Generate product URL (Kroger product page)
        if (!string.IsNullOrEmpty(product.ProductId))
        {
            result.ProductUrl = $"https://www.kroger.com/p/{product.ProductId}";
        }

        return result;
    }

    #endregion

    #region Barcode Helpers

    /// <summary>
    /// Normalize a barcode for Kroger API search.
    /// Removes check digit if valid and pads to 13 digits.
    /// </summary>
    private static string NormalizeBarcode(string barcode)
    {
        // Remove any non-numeric characters
        barcode = Regex.Replace(barcode, @"\D", "");

        if (string.IsNullOrEmpty(barcode))
        {
            return barcode;
        }

        // Check if barcode has a valid check digit
        if (barcode.Length == 12 || barcode.Length == 13)
        {
            if (ValidateUpcCheckDigit(barcode))
            {
                // Remove check digit
                barcode = barcode[..^1];
            }
        }

        // Zero-pad to 13 digits on the left
        return barcode.PadLeft(13, '0');
    }

    /// <summary>
    /// Validate UPC/EAN check digit.
    /// Works for both UPC-A (12 digits) and EAN-13 (13 digits).
    /// </summary>
    private static bool ValidateUpcCheckDigit(string barcode)
    {
        if (barcode.Length != 12 && barcode.Length != 13)
        {
            return false;
        }

        // Extract the check digit (last digit)
        var checkDigit = int.Parse(barcode[^1..]);

        // Get the data without check digit
        var data = barcode[..^1];

        var sum = 0;

        if (barcode.Length == 13)
        {
            // EAN-13: odd positions (0-indexed) get multiplied by 1, even by 3
            for (var i = 0; i < data.Length; i++)
            {
                var digit = int.Parse(data[i].ToString());
                sum += i % 2 == 0 ? digit : digit * 3;
            }
        }
        else
        {
            // UPC-A: odd positions (0-indexed) get multiplied by 3, even by 1
            for (var i = 0; i < data.Length; i++)
            {
                var digit = int.Parse(data[i].ToString());
                sum += i % 2 == 0 ? digit * 3 : digit;
            }
        }

        // Calculate expected check digit
        var expectedCheckDigit = (10 - (sum % 10)) % 10;

        return checkDigit == expectedCheckDigit;
    }

    #endregion

    #region Shopping Cart

    public async Task<ShoppingCartResult?> GetShoppingCartAsync(
        string accessToken,
        string storeLocationId,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        var url = $"{KrogerApiBaseUrl}/cart";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get cart. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);

            // Throw specific exception for auth failures to enable retry with token refresh
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new StoreAuthenticationException(
                    PluginId,
                    $"Authentication failed: {response.StatusCode}",
                    (int)response.StatusCode);
            }

            throw new InvalidOperationException($"Failed to get cart: {response.StatusCode}");
        }

        var cartResponse = JsonSerializer.Deserialize<KrogerCartResponse>(responseContent);
        if (cartResponse?.Data == null)
        {
            return new ShoppingCartResult { StoreLocationId = storeLocationId };
        }

        return MapToShoppingCartResult(cartResponse.Data, storeLocationId);
    }

    public async Task<ShoppingCartResult?> AddToCartAsync(
        string accessToken,
        string storeLocationId,
        List<CartItemRequest> items,
        CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Kroger plugin is not configured");
        }

        var krogerItems = items.Select(i => new KrogerCartItemUpdate
        {
            Upc = i.ExternalProductId,
            Quantity = i.Quantity
        }).ToList();

        var updateRequest = new KrogerCartUpdateRequest { Items = krogerItems };

        var url = $"{KrogerApiBaseUrl}/cart/add";

        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(updateRequest)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to add to cart. Status: {Status}, Response: {Response}",
                response.StatusCode, responseContent);

            // Throw specific exception for auth failures to enable retry with token refresh
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new StoreAuthenticationException(
                    PluginId,
                    $"Authentication failed: {response.StatusCode}",
                    (int)response.StatusCode);
            }

            throw new InvalidOperationException($"Failed to add to cart: {response.StatusCode}");
        }

        // Return updated cart
        return await GetShoppingCartAsync(accessToken, storeLocationId, ct);
    }

    public async Task<ShoppingCartResult?> UpdateCartItemAsync(
        string accessToken,
        string storeLocationId,
        string productId,
        int quantity,
        CancellationToken ct = default)
    {
        // Kroger cart API treats add/update the same - PUT with new quantity replaces
        return await AddToCartAsync(accessToken, storeLocationId, new List<CartItemRequest>
        {
            new() { ExternalProductId = productId, Quantity = quantity }
        }, ct);
    }

    public async Task<ShoppingCartResult?> RemoveFromCartAsync(
        string accessToken,
        string storeLocationId,
        string productId,
        CancellationToken ct = default)
    {
        // Kroger removes items by setting quantity to 0
        return await UpdateCartItemAsync(accessToken, storeLocationId, productId, 0, ct);
    }

    private static ShoppingCartResult MapToShoppingCartResult(KrogerCartData cartData, string storeLocationId)
    {
        var result = new ShoppingCartResult
        {
            StoreLocationId = storeLocationId,
            Items = new List<CartItemResult>()
        };

        if (cartData.Items != null)
        {
            foreach (var item in cartData.Items)
            {
                result.Items.Add(new CartItemResult
                {
                    ExternalProductId = item.Upc ?? item.ProductId ?? string.Empty,
                    Name = item.Description ?? string.Empty,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    ImageUrl = item.ImageUrl
                });
            }
        }

        result.Subtotal = cartData.Subtotal;

        return result;
    }

    public async Task ProcessPipelineAsync(ProductLookupPipelineContext context, CancellationToken ct = default)
    {
        var products = await SearchProductsAsync(null, null, context.Query, context.MaxResults, ct);

        // For barcode searches, normalize the search query for comparison
        string? normalizedSearchBarcode = null;
        if (context.SearchType == ProductLookupSearchType.Barcode)
        {
            normalizedSearchBarcode = ProductLookupPipelineContext.NormalizeBarcode(context.Query);
        }

        foreach(var product in products)
        {
            // For barcode searches, only include products whose normalized barcode matches the search
            // This filters out fuzzy/partial matches that Kroger's API may return
            if (context.SearchType == ProductLookupSearchType.Barcode &&
                !string.IsNullOrEmpty(normalizedSearchBarcode))
            {
                var productNormalizedBarcode = !string.IsNullOrEmpty(product.Barcode)
                    ? ProductLookupPipelineContext.NormalizeBarcode(product.Barcode)
                    : null;

                if (string.IsNullOrEmpty(productNormalizedBarcode) ||
                    !productNormalizedBarcode.Equals(normalizedSearchBarcode, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug(
                        "Skipping Kroger product with non-matching barcode. Search: {SearchBarcode} (normalized: {NormalizedSearch}), Product: {ProductBarcode} (normalized: {NormalizedProduct})",
                        context.Query, normalizedSearchBarcode, product.Barcode, productNormalizedBarcode);
                    continue; // Skip products that don't match the searched barcode
                }
            }

            // Use normalized barcode matching to handle different formats (UPC-A, EAN-13, Kroger internal)
            var resultItem = !string.IsNullOrEmpty(product.Barcode)
                ? context.FindMatchingResult(barcode: product.Barcode)
                : null;
            var isNewResult = resultItem == null;

            if (isNewResult)
            {
                resultItem = new ProductLookupResult
                {
                    Barcode = product.Barcode,
                    Name = product.Name,
                };
            }

            // Enrich result with Kroger data (both new and existing results)
            resultItem!.BrandName ??= product.Brand;
            resultItem.Description ??= product.Description;

            // Set image if not already set (first plugin to provide wins)
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                var krogerImage = new ResultImage { ImageUrl = product.ImageUrl, PluginId = DisplayName };
                resultItem.ImageUrl ??= krogerImage;
                resultItem.ThumbnailUrl ??= krogerImage;
            }

            foreach (var category in product.Categories)
            {
                if (!resultItem.Categories.Any(c => c.Equals(category, StringComparison.OrdinalIgnoreCase)))
                {
                    resultItem.Categories.Add(category);
                }
            }

            // Add Kroger as a data source if not already present
            resultItem.DataSources.TryAdd(DisplayName, product.ExternalProductId);

            if (isNewResult)
            {
                context.Results.Add(resultItem);
            }
        }
        
    }

    #endregion
}
