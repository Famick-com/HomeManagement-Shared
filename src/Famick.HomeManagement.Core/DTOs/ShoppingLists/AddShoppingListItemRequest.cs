namespace Famick.HomeManagement.Core.DTOs.ShoppingLists;

public class AddShoppingListItemRequest
{
    public Guid? ProductId { get; set; }
    public decimal Amount { get; set; } = 1;
    public string? Note { get; set; }
}
