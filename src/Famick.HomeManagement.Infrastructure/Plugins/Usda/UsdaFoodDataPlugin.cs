using System.Net.Http.Json;
using System.Text.Json;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Plugins.Usda;

/// <summary>
/// Built-in plugin for USDA FoodData Central API
/// https://fdc.nal.usda.gov/api-guide.html
/// </summary>
public class UsdaFoodDataPlugin : IProductLookupPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsdaFoodDataPlugin> _logger;
    private string _apiKey = string.Empty;
    private string _baseUrl = "https://api.nal.usda.gov/fdc/v1/";
    private int _defaultMaxResults = 20;
    private bool _isInitialized;

    public string PluginId => "usda";
    public string DisplayName => "USDA FoodData Central";
    public string Version => "1.0.0";
    public int Priority => 100;
    public bool IsAvailable => _isInitialized && !string.IsNullOrEmpty(_apiKey);

    public UsdaFoodDataPlugin(HttpClient httpClient, ILogger<UsdaFoodDataPlugin> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task InitAsync(JsonElement? pluginConfig, CancellationToken ct = default)
    {
        if (pluginConfig.HasValue)
        {
            var config = pluginConfig.Value;

            if (config.TryGetProperty("apiKey", out var apiKey))
            {
                _apiKey = apiKey.GetString() ?? string.Empty;
            }

            if (config.TryGetProperty("baseUrl", out var baseUrl))
            {
                _baseUrl = baseUrl.GetString() ?? _baseUrl;
            }

            if (config.TryGetProperty("defaultMaxResults", out var maxResults))
            {
                _defaultMaxResults = maxResults.GetInt32();
            }
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("USDA plugin: No API key configured. Plugin will not be available.");
        }
        else
        {
            _logger.LogInformation("USDA plugin initialized with base URL: {BaseUrl}", _baseUrl);
        }

        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<List<ProductLookupResult>> SearchByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("USDA plugin is not available (missing API key)");
            return new List<ProductLookupResult>();
        }

        _logger.LogInformation("Searching USDA by barcode: {Barcode}", barcode);

        try
        {
            // USDA uses gtinUpc field for barcodes, search for it
            var request = new UsdaSearchRequest
            {
                Query = barcode,
                DataType = new List<string> { "Branded" }, // Branded foods have barcodes
                PageSize = 10
            };

            var response = await SearchFoodsAsync(request, ct);
            if (response?.Foods == null || response.Foods.Count == 0)
            {
                return new List<ProductLookupResult>();
            }

            // Filter results to only those with matching barcode
            var matchingFoods = response.Foods
                .Where(f => !string.IsNullOrEmpty(f.GtinUpc) &&
                           f.GtinUpc.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return matchingFoods.Select(MapToLookupResult).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search USDA by barcode: {Barcode}", barcode);
            return new List<ProductLookupResult>();
        }
    }

    public async Task<List<ProductLookupResult>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("USDA plugin is not available (missing API key)");
            return new List<ProductLookupResult>();
        }

        _logger.LogInformation("Searching USDA by name: {Query}", query);

        try
        {
            var request = new UsdaSearchRequest
            {
                Query = query,
                DataType = new List<string> { "Branded", "Foundation", "SR Legacy" },
                PageSize = maxResults > 0 ? maxResults : _defaultMaxResults
            };

            var response = await SearchFoodsAsync(request, ct);
            if (response?.Foods == null || response.Foods.Count == 0)
            {
                return new List<ProductLookupResult>();
            }

            return response.Foods.Select(MapToLookupResult).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search USDA by name: {Query}", query);
            return new List<ProductLookupResult>();
        }
    }

    private async Task<UsdaSearchResponse?> SearchFoodsAsync(UsdaSearchRequest request, CancellationToken ct)
    {
        var url = $"{_baseUrl.TrimEnd('/')}/foods/search?api_key={_apiKey}";

        var response = await _httpClient.PostAsJsonAsync(url, request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UsdaSearchResponse>(ct);
    }

    private ProductLookupResult MapToLookupResult(UsdaSearchFood food)
    {
        var result = new ProductLookupResult
        {
            ExternalId = food.FdcId.ToString(),
            DataSource = PluginId,
            Name = food.Description,
            BrandName = food.BrandName,
            BrandOwner = food.BrandOwner,
            Barcode = food.GtinUpc,
            Category = food.FoodCategory,
            ServingSizeDescription = food.HouseholdServingFullText,
            Ingredients = food.Ingredients,
            Nutrition = MapNutrition(food)
        };

        return result;
    }

    private ProductLookupNutrition? MapNutrition(UsdaSearchFood food)
    {
        if (food.FoodNutrients == null || food.FoodNutrients.Count == 0)
        {
            return null;
        }

        var nutrition = new ProductLookupNutrition
        {
            ServingSize = food.ServingSize,
            ServingUnit = food.ServingSizeUnit
        };

        foreach (var nutrient in food.FoodNutrients)
        {
            if (nutrient.Value == null) continue;

            switch (nutrient.NutrientId)
            {
                case UsdaNutrientIds.Energy:
                    nutrition.Calories = nutrient.Value;
                    break;
                case UsdaNutrientIds.Protein:
                    nutrition.Protein = nutrient.Value;
                    break;
                case UsdaNutrientIds.TotalFat:
                    nutrition.TotalFat = nutrient.Value;
                    break;
                case UsdaNutrientIds.SaturatedFat:
                    nutrition.SaturatedFat = nutrient.Value;
                    break;
                case UsdaNutrientIds.TransFat:
                    nutrition.TransFat = nutrient.Value;
                    break;
                case UsdaNutrientIds.Cholesterol:
                    nutrition.Cholesterol = nutrient.Value;
                    break;
                case UsdaNutrientIds.Carbohydrates:
                    nutrition.TotalCarbohydrates = nutrient.Value;
                    break;
                case UsdaNutrientIds.Fiber:
                    nutrition.DietaryFiber = nutrient.Value;
                    break;
                case UsdaNutrientIds.TotalSugars:
                    nutrition.TotalSugars = nutrient.Value;
                    break;
                case UsdaNutrientIds.AddedSugars:
                    nutrition.AddedSugars = nutrient.Value;
                    break;
                case UsdaNutrientIds.Sodium:
                    nutrition.Sodium = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminA:
                    nutrition.VitaminA = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminC:
                    nutrition.VitaminC = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminD:
                    nutrition.VitaminD = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminE:
                    nutrition.VitaminE = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminK:
                    nutrition.VitaminK = nutrient.Value;
                    break;
                case UsdaNutrientIds.Thiamin:
                    nutrition.Thiamin = nutrient.Value;
                    break;
                case UsdaNutrientIds.Riboflavin:
                    nutrition.Riboflavin = nutrient.Value;
                    break;
                case UsdaNutrientIds.Niacin:
                    nutrition.Niacin = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminB6:
                    nutrition.VitaminB6 = nutrient.Value;
                    break;
                case UsdaNutrientIds.Folate:
                    nutrition.Folate = nutrient.Value;
                    break;
                case UsdaNutrientIds.VitaminB12:
                    nutrition.VitaminB12 = nutrient.Value;
                    break;
                case UsdaNutrientIds.Calcium:
                    nutrition.Calcium = nutrient.Value;
                    break;
                case UsdaNutrientIds.Iron:
                    nutrition.Iron = nutrient.Value;
                    break;
                case UsdaNutrientIds.Magnesium:
                    nutrition.Magnesium = nutrient.Value;
                    break;
                case UsdaNutrientIds.Phosphorus:
                    nutrition.Phosphorus = nutrient.Value;
                    break;
                case UsdaNutrientIds.Potassium:
                    nutrition.Potassium = nutrient.Value;
                    break;
                case UsdaNutrientIds.Zinc:
                    nutrition.Zinc = nutrient.Value;
                    break;
            }
        }

        return nutrition;
    }
}
