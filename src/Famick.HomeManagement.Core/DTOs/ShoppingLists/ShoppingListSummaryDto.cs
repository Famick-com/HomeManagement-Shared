namespace Famick.HomeManagement.Core.DTOs.ShoppingLists;

public class ShoppingListSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public DateTime UpdatedAt { get; set; }
}
