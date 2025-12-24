namespace Famick.HomeManagement.Core.DTOs.Recipes;

public class AddRecipePositionRequest
{
    public Guid ProductId { get; set; }
    public decimal Amount { get; set; }
    public Guid? QuantityUnitId { get; set; }
    public string? Note { get; set; }
    public string? IngredientGroup { get; set; }
    public bool OnlyCheckSingleUnitInStock { get; set; } = false;
    public bool NotCheckStockFulfillment { get; set; } = false;
}
