namespace Famick.HomeManagement.Core.DTOs.Recipes;

public class RecipeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<RecipePositionDto> Positions { get; set; } = new();
    public List<NestedRecipeDto> NestedRecipes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
