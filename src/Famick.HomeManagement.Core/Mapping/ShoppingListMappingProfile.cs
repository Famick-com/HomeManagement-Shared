using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ShoppingLists;
using Famick.HomeManagement.Domain.Entities;

namespace Famick.HomeManagement.Core.Mapping;

public class ShoppingListMappingProfile : Profile
{
    public ShoppingListMappingProfile()
    {
        CreateMap<ShoppingList, ShoppingListDto>()
            .ForMember(dest => dest.ShoppingLocationName,
                opt => opt.MapFrom(src => src.ShoppingLocation != null ? src.ShoppingLocation.Name : null))
            .ForMember(dest => dest.HasStoreIntegration,
                opt => opt.MapFrom(src => src.ShoppingLocation != null && !string.IsNullOrEmpty(src.ShoppingLocation.IntegrationType)))
            .ForMember(dest => dest.CanAddToCart,
                opt => opt.MapFrom(src => src.ShoppingLocation != null && !string.IsNullOrEmpty(src.ShoppingLocation.IntegrationType)))
            .ForMember(dest => dest.ItemCount,
                opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0))
            .ForMember(dest => dest.PurchasedCount,
                opt => opt.MapFrom(src => src.Items != null ? src.Items.Count(i => i.IsPurchased) : 0));

        CreateMap<ShoppingList, ShoppingListSummaryDto>()
            .ForMember(dest => dest.ShoppingLocationName,
                opt => opt.MapFrom(src => src.ShoppingLocation != null ? src.ShoppingLocation.Name : string.Empty))
            .ForMember(dest => dest.HasStoreIntegration,
                opt => opt.MapFrom(src => src.ShoppingLocation != null && !string.IsNullOrEmpty(src.ShoppingLocation.IntegrationType)))
            .ForMember(dest => dest.TotalItems,
                opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0))
            .ForMember(dest => dest.PurchasedItems,
                opt => opt.MapFrom(src => src.Items != null ? src.Items.Count(i => i.IsPurchased) : 0));

        CreateMap<ShoppingListItem, ShoppingListItemDto>()
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
            .ForMember(dest => dest.ProductImageUrl, opt => opt.Ignore()) // Set manually in service
            .ForMember(dest => dest.QuantityUnitName,
                opt => opt.MapFrom(src => src.Product != null && src.Product.QuantityUnitPurchase != null
                    ? src.Product.QuantityUnitPurchase.Name : null))
            .ForMember(dest => dest.Price, opt => opt.Ignore()); // Set manually from ProductStoreMetadata

        CreateMap<CreateShoppingListRequest, ShoppingList>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ShoppingLocation, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore());

        CreateMap<UpdateShoppingListRequest, ShoppingList>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ShoppingLocationId, opt => opt.Ignore()) // Cannot change store
            .ForMember(dest => dest.ShoppingLocation, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore());

        CreateMap<AddShoppingListItemRequest, ShoppingListItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.ShoppingListId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ShoppingList, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.IsPurchased, opt => opt.Ignore())
            .ForMember(dest => dest.PurchasedAt, opt => opt.Ignore());

        CreateMap<UpdateShoppingListItemRequest, ShoppingListItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.ShoppingListId, opt => opt.Ignore())
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ShoppingList, opt => opt.Ignore())
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.IsPurchased, opt => opt.Ignore())
            .ForMember(dest => dest.PurchasedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Aisle, opt => opt.Ignore())
            .ForMember(dest => dest.Shelf, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalProductId, opt => opt.Ignore());
    }
}
