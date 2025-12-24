using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Recipes;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public class RecipeService : IRecipeService
{
    private readonly HomeManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<RecipeService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    // Recipe management
    public async Task<RecipeDto> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating recipe: {Name}", request.Name);

        var recipe = _mapper.Map<Recipe>(request);
        recipe.Id = Guid.NewGuid();

        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created recipe: {Id} - {Name}", recipe.Id, recipe.Name);

        return _mapper.Map<RecipeDto>(recipe);
    }

    public async Task<RecipeDto?> GetByIdAsync(
        Guid id,
        bool includePositions = true,
        bool includeNesting = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Recipes.AsQueryable();

        if (includePositions)
        {
            query = query
                .Include(r => r.Positions!)
                    .ThenInclude(p => p.Product)
                .Include(r => r.Positions!)
                    .ThenInclude(p => p.QuantityUnit);
        }

        if (includeNesting)
        {
            query = query
                .Include(r => r.NestedRecipes!)
                    .ThenInclude(n => n.IncludedRecipe);
        }

        var recipe = await query.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return recipe != null ? _mapper.Map<RecipeDto>(recipe) : null;
    }

    public async Task<List<RecipeSummaryDto>> ListAsync(
        RecipeFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Recipes
            .Include(r => r.Positions)
            .Include(r => r.NestedRecipes)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(searchTerm) ||
                (r.Description != null && r.Description.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        query = (filter?.SortBy?.ToLower()) switch
        {
            "name" => filter.Descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),
            "createdat" => filter.Descending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt),
            "updatedat" => filter.Descending
                ? query.OrderByDescending(r => r.UpdatedAt)
                : query.OrderBy(r => r.UpdatedAt),
            _ => query.OrderBy(r => r.Name) // Default sort by name
        };

        var recipes = await query.ToListAsync(cancellationToken);

        return _mapper.Map<List<RecipeSummaryDto>>(recipes);
    }

    public async Task<RecipeDto> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating recipe: {Id}", id);

        var recipe = await _context.Recipes.FindAsync(new object[] { id }, cancellationToken);
        if (recipe == null)
        {
            throw new EntityNotFoundException(nameof(Recipe), id);
        }

        _mapper.Map(request, recipe);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated recipe: {Id} - {Name}", id, request.Name);

        // Reload with navigation properties
        recipe = await _context.Recipes
            .Include(r => r.Positions!)
                .ThenInclude(p => p.Product)
            .Include(r => r.Positions!)
                .ThenInclude(p => p.QuantityUnit)
            .Include(r => r.NestedRecipes!)
                .ThenInclude(n => n.IncludedRecipe)
            .FirstAsync(r => r.Id == id, cancellationToken);

        return _mapper.Map<RecipeDto>(recipe);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting recipe: {Id}", id);

        var recipe = await _context.Recipes.FindAsync(new object[] { id }, cancellationToken);
        if (recipe == null)
        {
            throw new EntityNotFoundException(nameof(Recipe), id);
        }

        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted recipe: {Id}", id);
    }

    // Recipe positions (ingredients)
    public async Task<RecipePositionDto> AddPositionAsync(
        Guid recipeId,
        AddRecipePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding position to recipe: {RecipeId}", recipeId);

        // Verify recipe exists
        var recipe = await _context.Recipes.FindAsync(new object[] { recipeId }, cancellationToken);
        if (recipe == null)
        {
            throw new EntityNotFoundException(nameof(Recipe), recipeId);
        }

        // Verify product exists
        var product = await _context.Products.FindAsync(new object[] { request.ProductId }, cancellationToken);
        if (product == null)
        {
            throw new EntityNotFoundException(nameof(Product), request.ProductId);
        }

        // GROCY TRIGGER MIGRATION: recipes_pos_qu_id_default
        // If QuantityUnitId is not provided, default to product's stock quantity unit
        var quantityUnitId = request.QuantityUnitId ?? product.QuantityUnitIdStock;

        var position = _mapper.Map<RecipePosition>(request);
        position.Id = Guid.NewGuid();
        position.TenantId = recipe.TenantId;
        position.RecipeId = recipeId;
        position.QuantityUnitId = quantityUnitId; // Apply trigger logic

        _context.RecipePositions.Add(position);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added recipe position for recipe {RecipeId}, product {ProductId}, QU defaulted: {DefaultApplied}",
            recipeId, request.ProductId, !request.QuantityUnitId.HasValue);

        // Reload with navigation properties
        position = await _context.RecipePositions
            .Include(p => p.Product)
            .Include(p => p.QuantityUnit)
            .FirstAsync(p => p.Id == position.Id, cancellationToken);

        return _mapper.Map<RecipePositionDto>(position);
    }

    public async Task<RecipePositionDto> UpdatePositionAsync(
        Guid positionId,
        UpdateRecipePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating recipe position: {PositionId}", positionId);

        var position = await _context.RecipePositions.FindAsync(new object[] { positionId }, cancellationToken);
        if (position == null)
        {
            throw new EntityNotFoundException(nameof(RecipePosition), positionId);
        }

        _mapper.Map(request, position);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated recipe position: {PositionId}", positionId);

        // Reload with navigation properties
        position = await _context.RecipePositions
            .Include(p => p.Product)
            .Include(p => p.QuantityUnit)
            .FirstAsync(p => p.Id == positionId, cancellationToken);

        return _mapper.Map<RecipePositionDto>(position);
    }

    public async Task RemovePositionAsync(
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing recipe position: {PositionId}", positionId);

        var position = await _context.RecipePositions.FindAsync(new object[] { positionId }, cancellationToken);
        if (position == null)
        {
            throw new EntityNotFoundException(nameof(RecipePosition), positionId);
        }

        _context.RecipePositions.Remove(position);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed recipe position: {PositionId}", positionId);
    }

    // Recipe nesting
    public async Task AddNestedRecipeAsync(
        Guid parentRecipeId,
        Guid childRecipeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding nested recipe: parent {ParentId}, child {ChildId}",
            parentRecipeId, childRecipeId);

        // Verify both recipes exist
        var parentRecipe = await _context.Recipes.FindAsync(new object[] { parentRecipeId }, cancellationToken);
        if (parentRecipe == null)
        {
            throw new EntityNotFoundException(nameof(Recipe), parentRecipeId);
        }

        var childRecipe = await _context.Recipes.FindAsync(new object[] { childRecipeId }, cancellationToken);
        if (childRecipe == null)
        {
            throw new EntityNotFoundException(nameof(Recipe), childRecipeId);
        }

        // Check for circular dependency BEFORE adding
        if (await WouldCreateCircularDependencyAsync(parentRecipeId, childRecipeId, cancellationToken))
        {
            throw new CircularDependencyException(nameof(Recipe), parentRecipeId);
        }

        var nesting = new RecipeNesting
        {
            Id = Guid.NewGuid(),
            TenantId = parentRecipe.TenantId,
            RecipeId = parentRecipeId,
            IncludesRecipeId = childRecipeId
        };

        _context.RecipeNestings.Add(nesting);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added nested recipe successfully");
    }

    public async Task RemoveNestedRecipeAsync(
        Guid parentRecipeId,
        Guid childRecipeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing nested recipe: parent {ParentId}, child {ChildId}",
            parentRecipeId, childRecipeId);

        var nesting = await _context.RecipeNestings
            .FirstOrDefaultAsync(rn => rn.RecipeId == parentRecipeId && rn.IncludesRecipeId == childRecipeId, cancellationToken);

        if (nesting == null)
        {
            throw new EntityNotFoundException("RecipeNesting",
                Guid.Parse("00000000-0000-0000-0000-000000000000")); // No specific ID for join table
        }

        _context.RecipeNestings.Remove(nesting);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed nested recipe successfully");
    }

    // Business logic
    public async Task<RecipeFulfillmentDto> CheckStockFulfillmentAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking stock fulfillment for recipe: {RecipeId}", recipeId);

        // Step 1: Get total flattened ingredients (handles nesting)
        var totalIngredients = await GetTotalIngredientsAsync(recipeId, cancellationToken);

        // Step 2: Check stock for each ingredient
        var fulfillments = new List<IngredientFulfillmentDto>();
        var allSufficient = true;

        foreach (var ingredient in totalIngredients)
        {
            // Skip if marked as NotCheckStockFulfillment
            var position = await _context.RecipePositions
                .FirstOrDefaultAsync(p => p.RecipeId == recipeId && p.ProductId == ingredient.ProductId, cancellationToken);

            if (position?.NotCheckStockFulfillment == true)
            {
                continue;
            }

            // Get current stock for product
            var stock = await _context.Stock
                .Where(s => s.ProductId == ingredient.ProductId)
                .SumAsync(s => s.Amount, cancellationToken);

            // TODO: Future enhancement - Convert stock to ingredient's quantity unit if needed
            var stockInIngredientUnit = stock;

            var isSufficient = stockInIngredientUnit >= ingredient.TotalAmount;
            allSufficient = allSufficient && isSufficient;

            fulfillments.Add(new IngredientFulfillmentDto
            {
                ProductId = ingredient.ProductId,
                ProductName = ingredient.ProductName,
                RequiredAmount = ingredient.TotalAmount,
                AvailableAmount = stockInIngredientUnit,
                IsSufficient = isSufficient,
                QuantityUnitName = ingredient.QuantityUnitName
            });
        }

        var recipe = await _context.Recipes.FindAsync(new object[] { recipeId }, cancellationToken);

        var result = new RecipeFulfillmentDto
        {
            RecipeId = recipeId,
            RecipeName = recipe?.Name ?? string.Empty,
            CanBeMade = allSufficient,
            Ingredients = fulfillments
        };

        _logger.LogInformation("Stock fulfillment check complete: CanBeMade={CanBeMade}", result.CanBeMade);

        return result;
    }

    public async Task<List<IngredientRequirementDto>> GetTotalIngredientsAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting total ingredients for recipe: {RecipeId}", recipeId);

        var visited = new HashSet<Guid>();
        var ingredients = new Dictionary<Guid, IngredientRequirementDto>();

        await CollectIngredientsRecursiveAsync(recipeId, 1.0m, visited, ingredients, cancellationToken);

        _logger.LogInformation("Total ingredients collected: {Count}", ingredients.Count);

        return ingredients.Values.ToList();
    }

    public async Task<List<Guid>> GetRecipeHierarchyAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        var hierarchy = new List<Guid>();
        var visited = new HashSet<Guid>();

        await CollectHierarchyRecursiveAsync(recipeId, hierarchy, visited, cancellationToken);

        return hierarchy;
    }

    // Private helper methods
    private async Task<bool> WouldCreateCircularDependencyAsync(
        Guid parentRecipeId,
        Guid childRecipeId,
        CancellationToken cancellationToken)
    {
        // If child is same as parent, it's circular
        if (childRecipeId == parentRecipeId)
        {
            return true;
        }

        // Check if parent is already nested in child's hierarchy
        var childHierarchy = await GetRecipeHierarchyAsync(childRecipeId, cancellationToken);
        return childHierarchy.Contains(parentRecipeId);
    }

    private async Task CollectIngredientsRecursiveAsync(
        Guid recipeId,
        decimal multiplier,
        HashSet<Guid> visited,
        Dictionary<Guid, IngredientRequirementDto> ingredients,
        CancellationToken cancellationToken)
    {
        // Cycle detection
        if (visited.Contains(recipeId))
        {
            throw new CircularDependencyException(nameof(Recipe), recipeId);
        }

        visited.Add(recipeId);

        // Get recipe with positions and nestings
        var recipe = await _context.Recipes
            .Include(r => r.Positions!)
                .ThenInclude(p => p.Product)
            .Include(r => r.Positions!)
                .ThenInclude(p => p.QuantityUnit)
            .Include(r => r.NestedRecipes!)
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

        if (recipe == null)
        {
            throw new EntityNotFoundException(nameof(Recipe), recipeId);
        }

        // Add direct ingredients
        if (recipe.Positions != null)
        {
            foreach (var position in recipe.Positions.Where(p => !p.NotCheckStockFulfillment))
            {
                var key = position.ProductId;
                var amount = position.Amount * multiplier;

                if (ingredients.ContainsKey(key))
                {
                    // Aggregate amounts (assuming same QU - conversion needed for mixed units)
                    ingredients[key].TotalAmount += amount;
                }
                else
                {
                    ingredients[key] = new IngredientRequirementDto
                    {
                        ProductId = position.ProductId,
                        ProductName = position.Product?.Name ?? string.Empty,
                        TotalAmount = amount,
                        QuantityUnitId = position.QuantityUnitId,
                        QuantityUnitName = position.QuantityUnit?.Name,
                        IngredientGroup = position.IngredientGroup
                    };
                }
            }
        }

        // Recursively process nested recipes
        if (recipe.NestedRecipes != null)
        {
            foreach (var nesting in recipe.NestedRecipes)
            {
                await CollectIngredientsRecursiveAsync(
                    nesting.IncludesRecipeId,
                    multiplier, // Could be extended to support servings multiplier
                    visited,
                    ingredients,
                    cancellationToken);
            }
        }

        visited.Remove(recipeId);
    }

    private async Task CollectHierarchyRecursiveAsync(
        Guid recipeId,
        List<Guid> hierarchy,
        HashSet<Guid> visited,
        CancellationToken cancellationToken)
    {
        if (visited.Contains(recipeId))
        {
            return; // Already visited, skip
        }

        visited.Add(recipeId);
        hierarchy.Add(recipeId);

        var nestings = await _context.RecipeNestings
            .Where(rn => rn.RecipeId == recipeId)
            .ToListAsync(cancellationToken);

        foreach (var nesting in nestings)
        {
            await CollectHierarchyRecursiveAsync(nesting.IncludesRecipeId, hierarchy, visited, cancellationToken);
        }
    }
}
