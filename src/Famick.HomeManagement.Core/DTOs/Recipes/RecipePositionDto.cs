namespace Famick.HomeManagement.Core.DTOs.Recipes;

public class RecipePositionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? QuantityUnitId { get; set; }
    public string? QuantityUnitName { get; set; }
    public string? Note { get; set; }
    public string? IngredientGroup { get; set; }
    public bool OnlyCheckSingleUnitInStock { get; set; }
    public bool NotCheckStockFulfillment { get; set; }
}
