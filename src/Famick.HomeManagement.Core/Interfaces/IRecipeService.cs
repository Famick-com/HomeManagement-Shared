using Famick.HomeManagement.Core.DTOs.Recipes;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for managing recipes, ingredients, and nested recipes
/// </summary>
public interface IRecipeService
{
    // Recipe management
    Task<RecipeDto> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken = default);

    Task<RecipeDto?> GetByIdAsync(
        Guid id,
        bool includePositions = true,
        bool includeNesting = true,
        CancellationToken cancellationToken = default);

    Task<List<RecipeSummaryDto>> ListAsync(
        RecipeFilterRequest? filter = null,
        CancellationToken cancellationToken = default);

    Task<RecipeDto> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Recipe positions (ingredients)
    Task<RecipePositionDto> AddPositionAsync(
        Guid recipeId,
        AddRecipePositionRequest request,
        CancellationToken cancellationToken = default);

    Task<RecipePositionDto> UpdatePositionAsync(
        Guid positionId,
        UpdateRecipePositionRequest request,
        CancellationToken cancellationToken = default);

    Task RemovePositionAsync(
        Guid positionId,
        CancellationToken cancellationToken = default);

    // Recipe nesting
    Task AddNestedRecipeAsync(
        Guid parentRecipeId,
        Guid childRecipeId,
        CancellationToken cancellationToken = default);

    Task RemoveNestedRecipeAsync(
        Guid parentRecipeId,
        Guid childRecipeId,
        CancellationToken cancellationToken = default);

    // Business logic
    Task<RecipeFulfillmentDto> CheckStockFulfillmentAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default);

    Task<List<IngredientRequirementDto>> GetTotalIngredientsAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> GetRecipeHierarchyAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default);
}
