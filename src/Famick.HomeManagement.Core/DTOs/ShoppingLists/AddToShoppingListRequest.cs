namespace Famick.HomeManagement.Core.DTOs.ShoppingLists;

/// <summary>
/// Request to add a product to a shopping list (from Stock Overview or barcode scan)
/// </summary>
public class AddToShoppingListRequest
{
    public Guid ShoppingListId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Barcode { get; set; }

    /// <summary>
    /// Product name for quick-add when no existing product is found
    /// </summary>
    public string? ProductName { get; set; }

    public decimal Amount { get; set; } = 1;
    public string? Note { get; set; }
    public bool LookupInStore { get; set; } = true;

    /// <summary>
    /// If true, mark the item as purchased immediately (for shopping mode app)
    /// </summary>
    public bool IsPurchased { get; set; }
}
