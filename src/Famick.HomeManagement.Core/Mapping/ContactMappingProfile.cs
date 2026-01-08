using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Contacts;
using Famick.HomeManagement.Domain.Entities;

namespace Famick.HomeManagement.Core.Mapping;

public class ContactMappingProfile : Profile
{
    public ContactMappingProfile()
    {
        // Contact -> ContactDto
        CreateMap<Contact, ContactDto>()
            .ForMember(dest => dest.LinkedUserName,
                opt => opt.MapFrom(src => src.LinkedUser != null
                    ? $"{src.LinkedUser.FirstName} {src.LinkedUser.LastName}"
                    : null))
            .ForMember(dest => dest.CreatedByUserName,
                opt => opt.MapFrom(src => $"{src.CreatedByUser.FirstName} {src.CreatedByUser.LastName}"))
            .ForMember(dest => dest.Addresses,
                opt => opt.MapFrom(src => src.Addresses))
            .ForMember(dest => dest.PhoneNumbers,
                opt => opt.MapFrom(src => src.PhoneNumbers))
            .ForMember(dest => dest.SocialMedia,
                opt => opt.MapFrom(src => src.SocialMedia))
            .ForMember(dest => dest.Tags,
                opt => opt.MapFrom(src => src.Tags.Select(t => t.Tag)))
            .ForMember(dest => dest.SharedWithUsers,
                opt => opt.MapFrom(src => src.SharedWithUsers))
            .ForMember(dest => dest.Relationships,
                opt => opt.MapFrom(src => src.RelationshipsAsSource));

        // Contact -> ContactSummaryDto
        CreateMap<Contact, ContactSummaryDto>()
            .ForMember(dest => dest.IsUserLinked,
                opt => opt.MapFrom(src => src.LinkedUserId.HasValue))
            .ForMember(dest => dest.PrimaryPhone,
                opt => opt.MapFrom(src => src.PhoneNumbers
                    .Where(p => p.IsPrimary)
                    .Select(p => p.PhoneNumber)
                    .FirstOrDefault() ?? src.PhoneNumbers
                    .OrderBy(p => p.CreatedAt)
                    .Select(p => p.PhoneNumber)
                    .FirstOrDefault()))
            .ForMember(dest => dest.PrimaryAddress,
                opt => opt.MapFrom(src => src.Addresses
                    .Where(a => a.IsPrimary)
                    .Select(a => a.Address.FormattedAddress ?? $"{a.Address.City}, {a.Address.StateProvince}")
                    .FirstOrDefault() ?? src.Addresses
                    .OrderBy(a => a.CreatedAt)
                    .Select(a => a.Address.FormattedAddress ?? $"{a.Address.City}, {a.Address.StateProvince}")
                    .FirstOrDefault()))
            .ForMember(dest => dest.TagNames,
                opt => opt.MapFrom(src => src.Tags.Select(t => t.Tag.Name).ToList()))
            .ForMember(dest => dest.TagColors,
                opt => opt.MapFrom(src => src.Tags.Select(t => t.Tag.Color).ToList()));

        // CreateContactRequest -> Contact
        CreateMap<CreateContactRequest, Contact>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HouseholdTenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LinkedUserId, opt => opt.Ignore())
            .ForMember(dest => dest.UsesTenantAddress, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.LinkedUser, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumbers, opt => opt.Ignore())
            .ForMember(dest => dest.SocialMedia, opt => opt.Ignore())
            .ForMember(dest => dest.RelationshipsAsSource, opt => opt.Ignore())
            .ForMember(dest => dest.RelationshipsAsTarget, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.SharedWithUsers, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());

        // UpdateContactRequest -> Contact
        CreateMap<UpdateContactRequest, Contact>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.HouseholdTenantId, opt => opt.Ignore())
            .ForMember(dest => dest.LinkedUserId, opt => opt.Ignore())
            .ForMember(dest => dest.UsesTenantAddress, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.LinkedUser, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.PhoneNumbers, opt => opt.Ignore())
            .ForMember(dest => dest.SocialMedia, opt => opt.Ignore())
            .ForMember(dest => dest.RelationshipsAsSource, opt => opt.Ignore())
            .ForMember(dest => dest.RelationshipsAsTarget, opt => opt.Ignore())
            .ForMember(dest => dest.Tags, opt => opt.Ignore())
            .ForMember(dest => dest.SharedWithUsers, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());

        // ContactAddress -> ContactAddressDto
        CreateMap<ContactAddress, ContactAddressDto>()
            .ForMember(dest => dest.IsTenantAddress,
                opt => opt.MapFrom(src => src.Contact != null && src.Contact.UsesTenantAddress && src.IsPrimary));

        // ContactPhoneNumber -> ContactPhoneNumberDto
        CreateMap<ContactPhoneNumber, ContactPhoneNumberDto>();

        // AddPhoneRequest -> ContactPhoneNumber
        CreateMap<AddPhoneRequest, ContactPhoneNumber>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.ContactId, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedNumber, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Contact, opt => opt.Ignore());

        // ContactSocialMedia -> ContactSocialMediaDto
        CreateMap<ContactSocialMedia, ContactSocialMediaDto>();

        // AddSocialMediaRequest -> ContactSocialMedia
        CreateMap<AddSocialMediaRequest, ContactSocialMedia>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.ContactId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Contact, opt => opt.Ignore());

        // ContactRelationship -> ContactRelationshipDto
        CreateMap<ContactRelationship, ContactRelationshipDto>()
            .ForMember(dest => dest.TargetContactName,
                opt => opt.MapFrom(src => src.TargetContact != null
                    ? (!string.IsNullOrWhiteSpace(src.TargetContact.PreferredName)
                        ? src.TargetContact.PreferredName
                        : $"{src.TargetContact.FirstName} {src.TargetContact.LastName}".Trim())
                    : string.Empty))
            .ForMember(dest => dest.TargetIsUserLinked,
                opt => opt.MapFrom(src => src.TargetContact != null && src.TargetContact.LinkedUserId.HasValue));

        // ContactTag -> ContactTagDto
        CreateMap<ContactTag, ContactTagDto>()
            .ForMember(dest => dest.ContactCount,
                opt => opt.MapFrom(src => src.Contacts.Count));

        // CreateContactTagRequest -> ContactTag
        CreateMap<CreateContactTagRequest, ContactTag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Contacts, opt => opt.Ignore());

        // UpdateContactTagRequest -> ContactTag
        CreateMap<UpdateContactTagRequest, ContactTag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Contacts, opt => opt.Ignore());

        // ContactUserShare -> ContactUserShareDto
        CreateMap<ContactUserShare, ContactUserShareDto>()
            .ForMember(dest => dest.SharedWithUserName,
                opt => opt.MapFrom(src => src.SharedWithUser != null
                    ? $"{src.SharedWithUser.FirstName} {src.SharedWithUser.LastName}"
                    : string.Empty));

        // ContactAuditLog -> ContactAuditLogDto
        CreateMap<ContactAuditLog, ContactAuditLogDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null
                    ? $"{src.User.FirstName} {src.User.LastName}"
                    : string.Empty));
    }
}
