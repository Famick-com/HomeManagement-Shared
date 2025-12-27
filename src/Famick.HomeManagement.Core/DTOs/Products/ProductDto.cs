namespace Famick.HomeManagement.Core.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public Guid QuantityUnitIdPurchase { get; set; }
    public string QuantityUnitPurchaseName { get; set; } = string.Empty;
    public Guid QuantityUnitIdStock { get; set; }
    public string QuantityUnitStockName { get; set; } = string.Empty;
    public decimal QuantityUnitFactorPurchaseToStock { get; set; }
    public decimal MinStockAmount { get; set; }
    public int DefaultBestBeforeDays { get; set; }
    public bool IsActive { get; set; }

    // Serving/package information
    public decimal? ServingSize { get; set; }
    public string? ServingUnit { get; set; }
    public decimal? ServingsPerContainer { get; set; }

    /// <summary>
    /// Calculated total weight in grams (ServingSize × ServingsPerContainer)
    /// Only valid when ServingUnit is "g"
    /// </summary>
    public decimal? TotalWeightGrams =>
        ServingSize.HasValue && ServingsPerContainer.HasValue &&
        string.Equals(ServingUnit, "g", StringComparison.OrdinalIgnoreCase)
            ? ServingSize.Value * ServingsPerContainer.Value
            : null;

    // Phase 2 properties
    public Guid? ProductGroupId { get; set; }
    public string? ProductGroupName { get; set; }
    public Guid? ShoppingLocationId { get; set; }
    public string? ShoppingLocationName { get; set; }

    // Related data
    public List<ProductBarcodeDto> Barcodes { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
