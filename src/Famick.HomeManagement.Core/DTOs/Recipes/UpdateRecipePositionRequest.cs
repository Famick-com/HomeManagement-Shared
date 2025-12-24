namespace Famick.HomeManagement.Core.DTOs.Recipes;

public class UpdateRecipePositionRequest
{
    public decimal Amount { get; set; }
    public Guid? QuantityUnitId { get; set; }
    public string? Note { get; set; }
    public string? IngredientGroup { get; set; }
    public bool OnlyCheckSingleUnitInStock { get; set; }
    public bool NotCheckStockFulfillment { get; set; }
}
