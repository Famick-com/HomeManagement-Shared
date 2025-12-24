namespace Famick.HomeManagement.Core.DTOs.Recipes;

public class RecipeSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int IngredientCount { get; set; }
    public int NestedRecipeCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
