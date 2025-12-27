using System.Text.RegularExpressions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// Service for searching products across all available plugins
/// </summary>
public class ProductLookupService : IProductLookupService
{
    private readonly IPluginLoader _pluginLoader;
    private readonly HomeManagementDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<ProductLookupService> _logger;

    // Regex for barcode detection: 8-14 digits (UPC-A, UPC-E, EAN-8, EAN-13, etc.)
    private static readonly Regex BarcodePattern = new(@"^[0-9]{8,14}$", RegexOptions.Compiled);

    public ProductLookupService(
        IPluginLoader pluginLoader,
        HomeManagementDbContext dbContext,
        ITenantProvider tenantProvider,
        ILogger<ProductLookupService> logger)
    {
        _pluginLoader = pluginLoader;
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Determines if the input looks like a barcode
    /// </summary>
    public static bool IsBarcode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var cleaned = input.Trim().Replace("-", "").Replace(" ", "");
        return BarcodePattern.IsMatch(cleaned);
    }

    public async Task<List<ProductLookupResult>> SearchAsync(string query, int maxResults = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<ProductLookupResult>();
        }

        // Auto-detect search type based on query format
        if (IsBarcode(query))
        {
            var cleanedBarcode = query.Trim().Replace("-", "").Replace(" ", "");
            return await SearchByBarcodeAsync(cleanedBarcode, ct);
        }

        return await SearchByNameAsync(query, maxResults, ct);
    }

    public async Task<List<ProductLookupResult>> SearchByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var plugins = _pluginLoader.GetAvailablePlugins();
        var results = new List<ProductLookupResult>();

        foreach (var plugin in plugins)
        {
            try
            {
                var pluginResults = await plugin.SearchByBarcodeAsync(barcode, ct);
                results.AddRange(pluginResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {PluginId} failed during barcode search", plugin.PluginId);
            }
        }

        return results;
    }

    public async Task<List<ProductLookupResult>> SearchByNameAsync(string query, int maxResults = 20, CancellationToken ct = default)
    {
        var plugins = _pluginLoader.GetAvailablePlugins();
        var results = new List<ProductLookupResult>();

        foreach (var plugin in plugins)
        {
            try
            {
                var pluginResults = await plugin.SearchByNameAsync(query, maxResults, ct);
                results.AddRange(pluginResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {PluginId} failed during name search", plugin.PluginId);
            }
        }

        return results;
    }

    public async Task ApplyLookupResultAsync(Guid productId, ProductLookupResult result, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId
            ?? throw new InvalidOperationException("Tenant context is required");

        var product = await _dbContext.Products
            .Include(p => p.Nutrition)
            .Include(p => p.Barcodes)
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId, ct);

        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found");
        }

        // Create or update nutrition data
        var nutrition = product.Nutrition ?? new ProductNutrition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId
        };

        nutrition.ExternalId = result.ExternalId;
        nutrition.DataSource = result.DataSource;
        nutrition.BrandOwner = result.BrandOwner;
        nutrition.BrandName = result.BrandName;
        nutrition.Ingredients = result.Ingredients;
        nutrition.ServingSizeDescription = result.ServingSizeDescription;
        nutrition.LastUpdatedFromSource = DateTime.UtcNow;

        if (result.Nutrition != null)
        {
            MapNutritionData(nutrition, result.Nutrition);

            // Also update Product serving fields from nutrition data
            if (result.Nutrition.ServingSize.HasValue)
            {
                product.ServingSize = result.Nutrition.ServingSize;
            }
            if (!string.IsNullOrEmpty(result.Nutrition.ServingUnit))
            {
                product.ServingUnit = result.Nutrition.ServingUnit;
            }
            if (result.Nutrition.ServingsPerContainer.HasValue)
            {
                product.ServingsPerContainer = result.Nutrition.ServingsPerContainer;
            }
        }

        if (product.Nutrition == null)
        {
            _dbContext.ProductNutrition.Add(nutrition);
        }

        // Add barcode if not already present
        if (!string.IsNullOrEmpty(result.Barcode))
        {
            var existingBarcode = product.Barcodes
                .FirstOrDefault(b => b.Barcode.Equals(result.Barcode, StringComparison.OrdinalIgnoreCase));

            if (existingBarcode == null)
            {
                _dbContext.ProductBarcodes.Add(new ProductBarcode
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProductId = productId,
                    Barcode = result.Barcode,
                    Note = $"From {result.DataSource}"
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public IReadOnlyList<PluginInfo> GetAvailablePlugins()
    {
        return _pluginLoader.Plugins
            .Select(p => new PluginInfo
            {
                PluginId = p.PluginId,
                DisplayName = p.DisplayName,
                Version = p.Version,
                IsAvailable = p.IsAvailable
            })
            .ToList()
            .AsReadOnly();
    }

    private static void MapNutritionData(ProductNutrition target, ProductLookupNutrition source)
    {
        target.ServingSize = source.ServingSize;
        target.ServingUnit = source.ServingUnit;
        target.ServingsPerContainer = source.ServingsPerContainer;
        target.Calories = source.Calories;
        target.TotalFat = source.TotalFat;
        target.SaturatedFat = source.SaturatedFat;
        target.TransFat = source.TransFat;
        target.Cholesterol = source.Cholesterol;
        target.Sodium = source.Sodium;
        target.TotalCarbohydrates = source.TotalCarbohydrates;
        target.DietaryFiber = source.DietaryFiber;
        target.TotalSugars = source.TotalSugars;
        target.AddedSugars = source.AddedSugars;
        target.Protein = source.Protein;
        target.VitaminA = source.VitaminA;
        target.VitaminC = source.VitaminC;
        target.VitaminD = source.VitaminD;
        target.VitaminE = source.VitaminE;
        target.VitaminK = source.VitaminK;
        target.Thiamin = source.Thiamin;
        target.Riboflavin = source.Riboflavin;
        target.Niacin = source.Niacin;
        target.VitaminB6 = source.VitaminB6;
        target.Folate = source.Folate;
        target.VitaminB12 = source.VitaminB12;
        target.Calcium = source.Calcium;
        target.Iron = source.Iron;
        target.Magnesium = source.Magnesium;
        target.Phosphorus = source.Phosphorus;
        target.Potassium = source.Potassium;
        target.Zinc = source.Zinc;
    }
}
