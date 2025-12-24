namespace Famick.HomeManagement.Core.DTOs.Recipes;

public class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
