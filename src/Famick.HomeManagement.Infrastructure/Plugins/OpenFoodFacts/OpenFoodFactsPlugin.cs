using System.Net.Http.Json;
using System.Text.Json;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Microsoft.Extensions.Logging;
using Npgsql.Replication;

namespace Famick.HomeManagement.Infrastructure.Plugins.OpenFoodFacts;

/// <summary>
/// Built-in plugin for Open Food Facts API.
/// Primarily enriches existing results with product images.
/// Should run after USDA in the pipeline to add images to results.
/// https://world.openfoodfacts.org/data
/// </summary>
public class OpenFoodFactsPlugin : IProductLookupPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsPlugin> _logger;
    private string _baseUrl = "https://world.openfoodfacts.org";
    private bool _isInitialized;

    public string PluginId => "openfoodfacts";
    public string DisplayName => "Open Food Facts";
    public string Version => "1.0.0";
    public bool IsAvailable => _isInitialized;

    public PluginAttribution? Attribution => new()
    {
        Url = "https://openfoodfacts.org",
        LicenseText = "Database: ODbL, Images: CC BY-SA",
        Description = "A free, open, collaborative database of food products from around the world. "
            + "Database contents are available under the Open Database License (ODbL). "
            + "Product images are available under the Creative Commons Attribution-ShareAlike (CC BY-SA) license.",
        ProductUrlTemplate = $"{_baseUrl.TrimEnd('/')}/product/{{barcode}}"
    };

    public OpenFoodFactsPlugin(IHttpClientFactory httpClientFactory, ILogger<OpenFoodFactsPlugin> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;

        // Set User-Agent as required by Open Food Facts API guidelines
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FamickHomeManagement/1.0 (https://github.com/Famick-com)");
    }

    public Task InitAsync(JsonElement? pluginConfig, CancellationToken ct = default)
    {
        if (pluginConfig.HasValue)
        {
            var config = pluginConfig.Value;

            if (config.TryGetProperty("baseUrl", out var baseUrl))
            {
                _baseUrl = baseUrl.GetString() ?? _baseUrl;
            }
        }

        _logger.LogInformation("OpenFoodFacts plugin initialized with base URL: {BaseUrl}", _baseUrl);
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task ProcessPipelineAsync(ProductLookupPipelineContext context, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("OpenFoodFacts plugin is not available");
            return;
        }

        _logger.LogInformation("OpenFoodFacts plugin processing pipeline: {Query} ({SearchType})",
            context.Query, context.SearchType);

        try
        {
            if (context.SearchType == ProductLookupSearchType.Barcode)
            {
                await EnrichByBarcodeAsync(context, ct);
            }
            else
            {
                await EnrichByNameAsync(context, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenFoodFacts plugin failed during pipeline processing: {Query}", context.Query);
        }
    }

    private async Task EnrichByBarcodeAsync(ProductLookupPipelineContext context, CancellationToken ct)
    {
        var product = await GetProductByBarcodeAsync(context.Query, ct);
        if (product == null)
        {
            _logger.LogDebug("No product found in OpenFoodFacts for barcode: {Barcode}", context.Query);
            return;
        }

        // Try to find existing result to enrich
        var existingResult = context.FindMatchingResult(barcode: context.Query);

        if (existingResult != null)
        {
            // Enrich with image URLs if missing
            EnrichWithImages(existingResult, product);
            EnrichWithAdditionalData(existingResult, product);
            existingResult.ProductUrl ??= !string.IsNullOrEmpty(product.Code)
                ? $"{_baseUrl.TrimEnd('/')}/product/{product.Code}"
                : null;
            _logger.LogDebug("Enriched existing result with OpenFoodFacts images for barcode: {Barcode}", context.Query);
        }
        else
        {
            // No existing result, add as new
            var newResult = MapToLookupResult(product);
            context.AddResult(newResult);
            _logger.LogDebug("Added new OpenFoodFacts result for barcode: {Barcode}", context.Query);
        }
    }

    private async Task EnrichByNameAsync(ProductLookupPipelineContext context, CancellationToken ct)
    {
        var products = await SearchProductsAsync(context.Query, context.MaxResults, ct);
        if (products == null || products.Count == 0)
        {
            _logger.LogDebug("No products found in OpenFoodFacts for query: {Query}", context.Query);
            return;
        }

        int enrichedCount = 0;
        int addedCount = 0;

        foreach (var product in products)
        {
            if (string.IsNullOrEmpty(product.Code)) continue;

            // Try to find existing result to enrich
            var existingResult = context.FindMatchingResult(barcode: product.Code);

            if (existingResult != null)
            {
                EnrichWithImages(existingResult, product);
                EnrichWithAdditionalData(existingResult, product);
                enrichedCount++;
            }
            else
            {
                // Only add new results if we have room
                if (context.Results.Count < context.MaxResults)
                {
                    var newResult = MapToLookupResult(product);
                    context.AddResult(newResult);
                    addedCount++;
                }
            }
        }

        _logger.LogDebug("OpenFoodFacts: enriched {Enriched} results, added {Added} new results for query: {Query}",
            enrichedCount, addedCount, context.Query);
    }

    private async Task<OpenFoodFactsProduct?> GetProductByBarcodeAsync(string barcode, CancellationToken ct)
    {
        try
        {
            var url = $"{_baseUrl.TrimEnd('/')}/api/v2/product/{barcode}.json";
            var response = await _httpClient.GetFromJsonAsync<OpenFoodFactsProductResponse>(url, ct);

            if (response?.Status == 1 && response.Product != null)
            {
                return response.Product;
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch product from OpenFoodFacts: {Barcode}", barcode);
            return null;
        }
    }

    private async Task<List<OpenFoodFactsProduct>?> SearchProductsAsync(string query, int maxResults, CancellationToken ct)
    {
        try
        {
            // Use the search endpoint
            var url = $"{_baseUrl.TrimEnd('/')}/cgi/search.pl?search_terms={Uri.EscapeDataString(query)}&page_size={maxResults}&json=1";
            var response = await _httpClient.GetFromJsonAsync<OpenFoodFactsSearchResponse>(url, ct);

            return response?.Products;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to search OpenFoodFacts: {Query}", query);
            return null;
        }
    }

    private void EnrichWithImages(ProductLookupResult result, OpenFoodFactsProduct product)
    {
        // Set image URL if not already set (first plugin wins)
        if (result.ImageUrl == null)
        {
            var imageUrl = product.ImageFrontUrl ?? product.ImageUrl;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                result.ImageUrl = new ResultImage { ImageUrl = imageUrl, PluginId = DisplayName };
            }
        }

        // Set thumbnail URL if missing (prefer small or thumb versions)
        if (result.ThumbnailUrl == null)
        {
            var thumbUrl = product.ImageThumbUrl
                ?? product.ImageFrontThumbUrl
                ?? product.ImageFrontSmallUrl
                ?? product.ImageSmallUrl;
            if (!string.IsNullOrEmpty(thumbUrl))
            {
                result.ThumbnailUrl = new ResultImage { ImageUrl = thumbUrl, PluginId = DisplayName };
            }
        }

        // Also enrich brand if missing
        if (string.IsNullOrEmpty(result.BrandName) && !string.IsNullOrEmpty(product.Brands))
        {
            result.BrandName = product.Brands;
        }
    }

    private static void EnrichWithAdditionalData(ProductLookupResult result, OpenFoodFactsProduct product)
    {
        result.AdditionalData ??= new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(product.NutriscoreGrade))
        {
            result.AdditionalData["nutriscore_grade"] = product.NutriscoreGrade;
        }

        if (product.NovaGroup.HasValue)
        {
            result.AdditionalData["nova_group"] = product.NovaGroup.Value;
        }
    }

    private ProductLookupResult MapToLookupResult(OpenFoodFactsProduct product)
    {
        var name = product.ProductNameEn ?? product.ProductName ?? "Unknown Product";

        var thumbUrl = product.ImageFrontSmallUrl ?? product.ImageSmallUrl ?? product.ImageFrontThumbUrl;

        var imageUrl = product.ImageFrontUrl ?? product.ImageUrl;

        var result = new ProductLookupResult
        {
            DataSources = { { DisplayName, product.Code ?? string.Empty } },
            Name = name,
            BrandName = product.Brands,
            Barcode = product.Code,
            Categories = product.CategoriesTags ?? new(),
            ThumbnailUrl = !string.IsNullOrEmpty(thumbUrl) ? new ResultImage { ImageUrl = thumbUrl, PluginId = DisplayName } : null,
            ImageUrl = !string.IsNullOrEmpty(imageUrl) ? new ResultImage { ImageUrl = imageUrl, PluginId = DisplayName } : null,
            Ingredients = product.IngredientsTextEn ?? product.IngredientsText,
            ServingSizeDescription = product.ServingSize,
            ProductUrl = !string.IsNullOrEmpty(product.Code)
                ? $"{_baseUrl.TrimEnd('/')}/product/{product.Code}"
                : null,
            Nutrition = MapNutrition(product),
            AdditionalData = new Dictionary<string, object>()
        };

        if (!string.IsNullOrEmpty(product.NutriscoreGrade))
        {
            result.AdditionalData["nutriscore_grade"] = product.NutriscoreGrade;
        }

        if (product.NovaGroup.HasValue)
        {
            result.AdditionalData["nova_group"] = product.NovaGroup.Value;
        }

        return result;
    }

    private static string? GetFirstCategory(List<string>? categoryTags)
    {
        if (categoryTags == null || categoryTags.Count == 0) return null;

        // Category tags are like "en:beverages", extract the readable part
        var firstTag = categoryTags.FirstOrDefault();
        if (firstTag == null) return null;

        var parts = firstTag.Split(':');
        if (parts.Length > 1)
        {
            return FormatCategoryName(parts[1]);
        }

        return FormatCategoryName(firstTag);
    }

    private static string FormatCategoryName(string category)
    {
        // Convert "plant-based-beverages" to "Plant Based Beverages"
        return string.Join(" ", category
            .Split('-')
            .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
    }

    private ProductLookupNutrition? MapNutrition(OpenFoodFactsProduct product)
    {
        var nutriments = product.Nutriments;
        if (nutriments == null) return null;

        // Prefer serving values, fall back to 100g values
        var nutrition = new ProductLookupNutrition
        {
            Source = PluginId,
            ExternalSourceId = product.Code,
            ServingSize = product.ServingQuantity,
            Calories = nutriments.EnergyKcalServing ?? nutriments.EnergyKcal100g,
            Protein = nutriments.ProteinsServing ?? nutriments.Proteins100g,
            TotalFat = nutriments.FatServing ?? nutriments.Fat100g,
            SaturatedFat = nutriments.SaturatedFatServing ?? nutriments.SaturatedFat100g,
            TotalCarbohydrates = nutriments.CarbohydratesServing ?? nutriments.Carbohydrates100g,
            TotalSugars = nutriments.SugarsServing ?? nutriments.Sugars100g,
            DietaryFiber = nutriments.FiberServing ?? nutriments.Fiber100g,
            Sodium = nutriments.SodiumServing ?? nutriments.Sodium100g
        };

        return nutrition;
    }
}
