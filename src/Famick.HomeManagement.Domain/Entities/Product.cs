namespace Famick.HomeManagement.Domain.Entities;

/// <summary>
/// Represents a product/item in the inventory system
/// </summary>
public class Product : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid LocationId { get; set; }
    public Guid QuantityUnitIdPurchase { get; set; }
    public Guid QuantityUnitIdStock { get; set; }
    public decimal QuantityUnitFactorPurchaseToStock { get; set; } = 1.0m;
    public decimal MinStockAmount { get; set; } = 0;
    public int DefaultBestBeforeDays { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Serving/package information (for weight calculations)
    public decimal? ServingSize { get; set; }
    public string? ServingUnit { get; set; }
    public decimal? ServingsPerContainer { get; set; }

    // Phase 2 - Product categorization and shopping
    public Guid? ProductGroupId { get; set; }
    public Guid? ShoppingLocationId { get; set; }

    // Navigation properties
    public Location Location { get; set; } = null!;
    public QuantityUnit QuantityUnitPurchase { get; set; } = null!;
    public QuantityUnit QuantityUnitStock { get; set; } = null!;
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    // Phase 2 navigation properties
    public ProductGroup? ProductGroup { get; set; }
    public ShoppingLocation? ShoppingLocation { get; set; }

    // Nutrition data (optional, populated from external sources)
    public ProductNutrition? Nutrition { get; set; }
}
