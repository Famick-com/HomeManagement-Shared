using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Home;
using Famick.HomeManagement.Core.DTOs.Vehicles;
using Famick.HomeManagement.Core.DTOs.Wizard;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public class WizardService : IWizardService
{
    private readonly HomeManagementDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IMapper _mapper;
    private readonly ILogger<WizardService> _logger;

    public WizardService(
        HomeManagementDbContext context,
        ITenantProvider tenantProvider,
        IMapper mapper,
        ILogger<WizardService> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<WizardStateDto> GetWizardStateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting wizard state for current tenant");

        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant ID is required");
        }

        // Get tenant info (Page 1)
        var tenant = await _context.Tenants
            .Include(t => t.Address)
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);

        // Get home info (Page 3)
        var home = await _context.Homes
            .Include(h => h.PropertyLinks)
            .FirstOrDefaultAsync(cancellationToken);

        // Get household members (Page 2)
        var members = await GetHouseholdMembersAsync(cancellationToken);

        // Get vehicles (Page 5)
        var vehicles = await _context.Vehicles
            .Include(v => v.PrimaryDriver)
            .Where(v => v.IsActive)
            .OrderBy(v => v.Year)
            .ThenBy(v => v.Make)
            .ThenBy(v => v.Model)
            .ToListAsync(cancellationToken);

        return new WizardStateDto
        {
            IsComplete = home?.IsSetupComplete ?? false,
            HouseholdInfo = new HouseholdInfoDto
            {
                TenantId = tenantId.Value,
                Name = tenant?.Name ?? string.Empty,
                Street1 = tenant?.Address?.AddressLine1,
                Street2 = tenant?.Address?.AddressLine2,
                City = tenant?.Address?.City,
                State = tenant?.Address?.StateProvince,
                PostalCode = tenant?.Address?.PostalCode,
                Country = tenant?.Address?.Country,
                IsAddressNormalized = !string.IsNullOrEmpty(tenant?.Address?.NormalizedHash)
            },
            HouseholdMembers = members,
            HomeStatistics = new HomeStatisticsDto
            {
                HomeId = home?.Id,
                SquareFootage = home?.SquareFootage,
                YearBuilt = home?.YearBuilt,
                Bedrooms = home?.Bedrooms,
                Bathrooms = home?.Bathrooms,
                Unit = home?.Unit,
                HoaName = home?.HoaName,
                HoaContactInfo = home?.HoaContactInfo,
                HoaRulesLink = home?.HoaRulesLink,
                PropertyLinks = home?.PropertyLinks
                    .OrderBy(p => p.SortOrder)
                    .Select(p => new PropertyLinkDto
                    {
                        Id = p.Id,
                        HomeId = p.HomeId,
                        Url = p.Url,
                        Label = p.Label,
                        SortOrder = p.SortOrder,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToList() ?? new List<PropertyLinkDto>()
            },
            MaintenanceItems = new MaintenanceItemsDto
            {
                AcFilterSizes = home?.AcFilterSizes,
                FridgeWaterFilterType = home?.FridgeWaterFilterType,
                UnderSinkFilterType = home?.UnderSinkFilterType,
                WholeHouseFilterType = home?.WholeHouseFilterType,
                SmokeCoDetectorBatteryType = home?.SmokeCoDetectorBatteryType
            },
            Vehicles = vehicles.Select(v => new VehicleSummaryDto
            {
                Id = v.Id,
                Year = v.Year,
                Make = v.Make,
                Model = v.Model,
                Trim = v.Trim,
                LicensePlate = v.LicensePlate,
                Color = v.Color,
                CurrentMileage = v.CurrentMileage,
                PrimaryDriverName = v.PrimaryDriver?.DisplayName,
                IsActive = v.IsActive,
                DisplayName = v.DisplayName
            }).ToList()
        };
    }

    public async Task<List<HouseholdMemberDto>> GetHouseholdMembersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting household members");

        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant ID is required");
        }

        // Get contacts that belong to this household
        var contacts = await _context.Contacts
            .Include(c => c.LinkedUser)
            .Where(c => c.HouseholdTenantId == tenantId.Value || c.TenantId == tenantId.Value)
            .Where(c => c.IsActive)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync(cancellationToken);

        // Get current user's contact to determine which one is "self"
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId.Value, cancellationToken);

        return contacts.Select(c => new HouseholdMemberDto
        {
            ContactId = c.Id,
            FirstName = c.FirstName ?? string.Empty,
            LastName = c.LastName,
            DisplayName = c.DisplayName,
            ProfileImageFileName = c.ProfileImageFileName,
            RelationshipType = c.LinkedUserId.HasValue && c.LinkedUserId == currentUser?.Id ? "Self" : null,
            IsCurrentUser = c.LinkedUserId.HasValue && c.LinkedUserId == currentUser?.Id,
            HasUserAccount = c.LinkedUserId.HasValue,
            Email = c.LinkedUser?.Email
        }).ToList();
    }

    public async Task<HouseholdMemberDto> AddHouseholdMemberAsync(
        AddHouseholdMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding household member: {FirstName} {LastName}", request.FirstName, request.LastName);

        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant ID is required");
        }

        Contact contact;

        if (request.ExistingContactId.HasValue)
        {
            // Link existing contact to household
            contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == request.ExistingContactId.Value, cancellationToken);

            if (contact == null)
            {
                throw new EntityNotFoundException(nameof(Contact), request.ExistingContactId.Value);
            }

            // Update household assignment
            contact.HouseholdTenantId = tenantId.Value;
        }
        else
        {
            // Get current user for CreatedByUserId
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenantId.Value, cancellationToken);

            if (currentUser == null)
            {
                throw new InvalidOperationException("Current user not found");
            }

            // Create new contact
            contact = new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                HouseholdTenantId = tenantId.Value,
                CreatedByUserId = currentUser.Id,
                IsActive = true
            };

            _context.Contacts.Add(contact);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Household member added: {ContactId}", contact.Id);

        return new HouseholdMemberDto
        {
            ContactId = contact.Id,
            FirstName = contact.FirstName ?? string.Empty,
            LastName = contact.LastName,
            DisplayName = contact.DisplayName,
            ProfileImageFileName = contact.ProfileImageFileName,
            RelationshipType = request.RelationshipType,
            IsCurrentUser = false,
            HasUserAccount = contact.LinkedUserId.HasValue
        };
    }

    public async Task<HouseholdMemberDto> UpdateHouseholdMemberAsync(
        Guid contactId,
        UpdateHouseholdMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating household member: {ContactId}", contactId);

        var contact = await _context.Contacts
            .Include(c => c.LinkedUser)
            .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);

        if (contact == null)
        {
            throw new EntityNotFoundException(nameof(Contact), contactId);
        }

        // Note: Relationship is typically stored in ContactRelationship entity,
        // but for wizard simplicity, we're just returning the DTO with the relationship
        // A full implementation would create/update ContactRelationship entries

        await _context.SaveChangesAsync(cancellationToken);

        var tenantId = _tenantProvider.TenantId;
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenantId.Value, cancellationToken);

        return new HouseholdMemberDto
        {
            ContactId = contact.Id,
            FirstName = contact.FirstName ?? string.Empty,
            LastName = contact.LastName,
            DisplayName = contact.DisplayName,
            ProfileImageFileName = contact.ProfileImageFileName,
            RelationshipType = request.RelationshipType,
            IsCurrentUser = contact.LinkedUserId.HasValue && contact.LinkedUserId == currentUser?.Id,
            HasUserAccount = contact.LinkedUserId.HasValue,
            Email = contact.LinkedUser?.Email
        };
    }

    public async Task RemoveHouseholdMemberAsync(
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing household member: {ContactId}", contactId);

        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId, cancellationToken);

        if (contact == null)
        {
            throw new EntityNotFoundException(nameof(Contact), contactId);
        }

        // Unlink from household instead of deleting
        contact.HouseholdTenantId = null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Household member removed: {ContactId}", contactId);
    }

    public async Task<DuplicateContactResultDto> CheckDuplicateContactAsync(
        CheckDuplicateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking for duplicate contacts: {FirstName} {LastName}", request.FirstName, request.LastName);

        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant ID is required");
        }

        // Search for contacts with matching name
        var query = _context.Contacts
            .Where(c => c.IsActive);

        // Exact match on first name (case-insensitive)
        query = query.Where(c => c.FirstName != null &&
            c.FirstName.ToLower() == request.FirstName.ToLower());

        // If last name provided, filter by it too
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            query = query.Where(c => c.LastName != null &&
                c.LastName.ToLower() == request.LastName.ToLower());
        }

        var matches = await query
            .Take(10)
            .ToListAsync(cancellationToken);

        return new DuplicateContactResultDto
        {
            HasDuplicates = matches.Count != 0,
            Matches = matches.Select(c => new DuplicateContactMatchDto
            {
                ContactId = c.Id,
                FirstName = c.FirstName ?? string.Empty,
                LastName = c.LastName,
                DisplayName = c.DisplayName,
                ProfileImageFileName = c.ProfileImageFileName,
                IsHouseholdMember = c.HouseholdTenantId == tenantId.Value,
                MatchType = "Exact"
            }).ToList()
        };
    }

    public async Task CompleteWizardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing wizard");

        var home = await _context.Homes.FirstOrDefaultAsync(cancellationToken);

        if (home == null)
        {
            // Create minimal home record if it doesn't exist
            home = new Home
            {
                Id = Guid.NewGuid(),
                IsSetupComplete = true
            };
            _context.Homes.Add(home);
        }
        else
        {
            home.IsSetupComplete = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wizard completed");
    }
}
