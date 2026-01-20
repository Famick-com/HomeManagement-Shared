namespace Famick.HomeManagement.Core.DTOs.ShoppingLists;

public class ShoppingListItemDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductImageUrl { get; set; }
    public decimal Amount { get; set; }
    public string? QuantityUnitName { get; set; }
    public string? Note { get; set; }
    public bool IsPurchased { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime? BestBeforeDate { get; set; }

    // Product tracking fields for date prompting logic
    public bool TracksBestBeforeDate { get; set; }
    public int DefaultBestBeforeDays { get; set; }
    public Guid? DefaultLocationId { get; set; }

    public string? Aisle { get; set; }
    public string? Shelf { get; set; }
    public string? Department { get; set; }
    public string? ExternalProductId { get; set; }
    public decimal? Price { get; set; }

    // Store integration fields (for items without linked products)
    public string? ImageUrl { get; set; }
    public string? Barcode { get; set; }
}
