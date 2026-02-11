using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Recipes;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

// TODO: Rewrite for step-based recipe model (Phase 2)
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

    public Task<RecipeDto> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<RecipeDto?> GetByIdAsync(Guid id, bool includePositions = true, bool includeNesting = true, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<List<RecipeSummaryDto>> ListAsync(RecipeFilterRequest? filter = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<RecipeDto> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<RecipePositionDto> AddPositionAsync(Guid recipeId, AddRecipePositionRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<RecipePositionDto> UpdatePositionAsync(Guid positionId, UpdateRecipePositionRequest request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task RemovePositionAsync(Guid positionId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task AddNestedRecipeAsync(Guid parentRecipeId, Guid childRecipeId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task RemoveNestedRecipeAsync(Guid parentRecipeId, Guid childRecipeId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<RecipeFulfillmentDto> CheckStockFulfillmentAsync(Guid recipeId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<List<IngredientRequirementDto>> GetTotalIngredientsAsync(Guid recipeId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");

    public Task<List<Guid>> GetRecipeHierarchyAsync(Guid recipeId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException("Recipe service is being redesigned for step-based model.");
}
