using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ShoppingLocations;
using Famick.HomeManagement.Domain.Entities;

namespace Famick.HomeManagement.Core.Mapping;

public class ShoppingLocationMappingProfile : Profile
{
    public ShoppingLocationMappingProfile()
    {
        CreateMap<ShoppingLocation, ShoppingLocationDto>()
            .ForMember(dest => dest.ProductCount,
                opt => opt.MapFrom(src => src.Products != null ? src.Products.Count : 0))
            .ForMember(dest => dest.IsConnected,
                opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.OAuthAccessToken) &&
                    src.OAuthTokenExpiresAt.HasValue &&
                    src.OAuthTokenExpiresAt.Value > DateTime.UtcNow));

        CreateMap<CreateShoppingLocationRequest, ShoppingLocation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.ProductStoreMetadata, opt => opt.Ignore())
            // Ignore integration fields for create
            .ForMember(dest => dest.IntegrationType, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalLocationId, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalChainId, opt => opt.Ignore())
            .ForMember(dest => dest.OAuthAccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.OAuthRefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.OAuthTokenExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.StoreAddress, opt => opt.Ignore())
            .ForMember(dest => dest.StorePhone, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore());

        CreateMap<UpdateShoppingLocationRequest, ShoppingLocation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.ProductStoreMetadata, opt => opt.Ignore())
            // Ignore integration fields for update (handled by separate API)
            .ForMember(dest => dest.IntegrationType, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalLocationId, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalChainId, opt => opt.Ignore())
            .ForMember(dest => dest.OAuthAccessToken, opt => opt.Ignore())
            .ForMember(dest => dest.OAuthRefreshToken, opt => opt.Ignore())
            .ForMember(dest => dest.OAuthTokenExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.StoreAddress, opt => opt.Ignore())
            .ForMember(dest => dest.StorePhone, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore());
    }
}
