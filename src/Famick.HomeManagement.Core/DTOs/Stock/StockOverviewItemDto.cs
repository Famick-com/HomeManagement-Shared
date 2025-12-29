namespace Famick.HomeManagement.Core.DTOs.Stock;

/// <summary>
/// Aggregated product stock view for the stock overview page.
/// One row per product with totals across all stock entries.
/// </summary>
public class StockOverviewItemDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product group ID (optional).
    /// </summary>
    public Guid? ProductGroupId { get; set; }

    /// <summary>
    /// Product group name (optional).
    /// </summary>
    public string? ProductGroupName { get; set; }

    /// <summary>
    /// Total amount in stock across all entries.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Name of the stock quantity unit.
    /// </summary>
    public string QuantityUnitName { get; set; } = string.Empty;

    /// <summary>
    /// Earliest best before date among all stock entries.
    /// </summary>
    public DateTime? NextDueDate { get; set; }

    /// <summary>
    /// Days until the next due date (negative if past).
    /// </summary>
    public int? DaysUntilDue { get; set; }

    /// <summary>
    /// Total value of all stock for this product.
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Minimum stock amount defined for this product.
    /// </summary>
    public decimal MinStockAmount { get; set; }

    /// <summary>
    /// True if total amount is below minimum stock amount.
    /// </summary>
    public bool IsBelowMinStock { get; set; }

    /// <summary>
    /// True if any stock entry has expired (BestBeforeDate < Today).
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// True if expiring within the next 5 days.
    /// </summary>
    public bool IsDueSoon { get; set; }

    /// <summary>
    /// Number of individual stock entries for this product.
    /// </summary>
    public int StockEntryCount { get; set; }
}
