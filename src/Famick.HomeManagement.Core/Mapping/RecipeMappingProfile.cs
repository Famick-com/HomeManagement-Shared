using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Recipes;
using Famick.HomeManagement.Domain.Entities;

namespace Famick.HomeManagement.Core.Mapping;

public class RecipeMappingProfile : Profile
{
    public RecipeMappingProfile()
    {
        CreateMap<Recipe, RecipeDto>()
            .ForMember(dest => dest.Positions,
                opt => opt.MapFrom(src => src.Positions ?? new List<RecipePosition>()))
            .ForMember(dest => dest.NestedRecipes,
                opt => opt.MapFrom(src => src.NestedRecipes ?? new List<RecipeNesting>()));

        CreateMap<Recipe, RecipeSummaryDto>()
            .ForMember(dest => dest.IngredientCount,
                opt => opt.MapFrom(src => src.Positions != null ? src.Positions.Count : 0))
            .ForMember(dest => dest.NestedRecipeCount,
                opt => opt.MapFrom(src => src.NestedRecipes != null ? src.NestedRecipes.Count : 0));

        CreateMap<RecipePosition, RecipePositionDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
            .ForMember(dest => dest.QuantityUnitName,
                opt => opt.MapFrom(src => src.QuantityUnit != null ? src.QuantityUnit.Name : null));

        CreateMap<RecipeNesting, NestedRecipeDto>()
            .ForMember(dest => dest.RecipeId,
                opt => opt.MapFrom(src => src.IncludesRecipeId))
            .ForMember(dest => dest.RecipeName,
                opt => opt.MapFrom(src => src.IncludedRecipe != null ? src.IncludedRecipe.Name : string.Empty));

        CreateMap<CreateRecipeRequest, Recipe>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Positions, opt => opt.Ignore())
            .ForMember(dest => dest.NestedRecipes, opt => opt.Ignore())
            .ForMember(dest => dest.ParentRecipes, opt => opt.Ignore());

        CreateMap<UpdateRecipeRequest, Recipe>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Positions, opt => opt.Ignore())
            .ForMember(dest => dest.NestedRecipes, opt => opt.Ignore())
            .ForMember(dest => dest.ParentRecipes, opt => opt.Ignore());

        CreateMap<AddRecipePositionRequest, RecipePosition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Recipe, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.QuantityUnit, opt => opt.Ignore());

        CreateMap<UpdateRecipePositionRequest, RecipePosition>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.RecipeId, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Recipe, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.QuantityUnit, opt => opt.Ignore());
    }
}
