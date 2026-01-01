using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ProductGroups;
using Famick.HomeManagement.Core.DTOs.ShoppingLocations;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public class ShoppingLocationService : IShoppingLocationService
{
    private readonly HomeManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ShoppingLocationService> _logger;
    private readonly IStoreIntegrationLoader _storeIntegrationLoader;

    public ShoppingLocationService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<ShoppingLocationService> logger,
        IStoreIntegrationLoader storeIntegrationLoader)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _storeIntegrationLoader = storeIntegrationLoader;
    }

    public async Task<ShoppingLocationDto> CreateAsync(
        CreateShoppingLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating shopping location: {Name}", request.Name);

        // Check for duplicate name
        var exists = await _context.ShoppingLocations
            .AnyAsync(sl => sl.Name == request.Name, cancellationToken);

        if (exists)
        {
            throw new DuplicateEntityException(nameof(ShoppingLocation), "Name", request.Name);
        }

        var shoppingLocation = _mapper.Map<ShoppingLocation>(request);
        shoppingLocation.Id = Guid.NewGuid();

        _context.ShoppingLocations.Add(shoppingLocation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created shopping location: {Id} - {Name}", shoppingLocation.Id, shoppingLocation.Name);

        var dto = _mapper.Map<ShoppingLocationDto>(shoppingLocation);
        await PopulateIsConnectedAsync(new[] { dto }, cancellationToken);
        return dto;
    }

    public async Task<ShoppingLocationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var shoppingLocation = await _context.ShoppingLocations
            .Include(sl => sl.Products)
            .FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);

        if (shoppingLocation == null)
            return null;

        var dto = _mapper.Map<ShoppingLocationDto>(shoppingLocation);
        await PopulateIsConnectedAsync(new[] { dto }, cancellationToken);
        return dto;
    }

    public async Task<List<ShoppingLocationDto>> ListAsync(
        ShoppingLocationFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShoppingLocations
            .Include(sl => sl.Products)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(sl =>
                sl.Name.ToLower().Contains(searchTerm) ||
                (sl.Description != null && sl.Description.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        query = (filter?.SortBy?.ToLower()) switch
        {
            "name" => filter.Descending
                ? query.OrderByDescending(sl => sl.Name)
                : query.OrderBy(sl => sl.Name),
            "createdat" => filter.Descending
                ? query.OrderByDescending(sl => sl.CreatedAt)
                : query.OrderBy(sl => sl.CreatedAt),
            "productcount" => filter.Descending
                ? query.OrderByDescending(sl => sl.Products!.Count)
                : query.OrderBy(sl => sl.Products!.Count),
            _ => query.OrderBy(sl => sl.Name) // Default sort by name
        };

        var shoppingLocations = await query.ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ShoppingLocationDto>>(shoppingLocations);
        await PopulateIsConnectedAsync(dtos, cancellationToken);
        return dtos;
    }

    public async Task<ShoppingLocationDto> UpdateAsync(
        Guid id,
        UpdateShoppingLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating shopping location: {Id}", id);

        var shoppingLocation = await _context.ShoppingLocations.FindAsync(new object[] { id }, cancellationToken);
        if (shoppingLocation == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingLocation), id);
        }

        // Check for duplicate name (excluding current entity)
        var exists = await _context.ShoppingLocations
            .AnyAsync(sl => sl.Name == request.Name && sl.Id != id, cancellationToken);

        if (exists)
        {
            throw new DuplicateEntityException(nameof(ShoppingLocation), "Name", request.Name);
        }

        _mapper.Map(request, shoppingLocation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated shopping location: {Id} - {Name}", id, request.Name);

        // Reload with products for DTO mapping
        shoppingLocation = await _context.ShoppingLocations
            .Include(sl => sl.Products)
            .FirstAsync(sl => sl.Id == id, cancellationToken);

        var dto = _mapper.Map<ShoppingLocationDto>(shoppingLocation);
        await PopulateIsConnectedAsync(new[] { dto }, cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting shopping location: {Id}", id);

        var shoppingLocation = await _context.ShoppingLocations.FindAsync(new object[] { id }, cancellationToken);
        if (shoppingLocation == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingLocation), id);
        }

        _context.ShoppingLocations.Remove(shoppingLocation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted shopping location: {Id}", id);
    }

    public async Task<List<ProductSummaryDto>> GetProductsAtLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        var shoppingLocation = await _context.ShoppingLocations.FindAsync(new object[] { locationId }, cancellationToken);
        if (shoppingLocation == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingLocation), locationId);
        }

        var products = await _context.Products
            .Include(p => p.ProductGroup)
            .Include(p => p.ShoppingLocation)
            .Where(p => p.ShoppingLocationId == locationId)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<ProductSummaryDto>>(products);
    }

    /// <summary>
    /// Populates the IsConnected property on ShoppingLocationDto instances.
    /// For plugins that don't require OAuth, IsConnected is always true.
    /// For OAuth plugins, checks the TenantIntegrationTokens table for valid tokens.
    /// </summary>
    private async Task PopulateIsConnectedAsync(
        IEnumerable<ShoppingLocationDto> dtos,
        CancellationToken cancellationToken)
    {
        // Get unique integration types that have shopping locations
        var integrationTypes = dtos
            .Where(d => !string.IsNullOrEmpty(d.IntegrationType))
            .Select(d => d.IntegrationType!)
            .Distinct()
            .ToList();

        if (integrationTypes.Count == 0)
            return;

        // Build a set of plugins that don't require OAuth (always connected)
        var noOAuthPlugins = _storeIntegrationLoader.Plugins
            .Where(p => integrationTypes.Contains(p.PluginId) && !p.Capabilities.RequiresOAuth)
            .Select(p => p.PluginId)
            .ToHashSet();

        // Get OAuth-requiring integration types
        var oauthIntegrationTypes = integrationTypes
            .Where(t => !noOAuthPlugins.Contains(t))
            .ToList();

        // Query tokens only for OAuth-requiring plugins
        var connectedOAuthPlugins = new HashSet<string>();
        if (oauthIntegrationTypes.Count > 0)
        {
            var tokens = await _context.TenantIntegrationTokens
                .Where(t => oauthIntegrationTypes.Contains(t.PluginId))
                .ToListAsync(cancellationToken);

            connectedOAuthPlugins = tokens
                .Where(t => !string.IsNullOrEmpty(t.AccessToken) &&
                            !t.RequiresReauth &&
                            t.ExpiresAt.HasValue &&
                            t.ExpiresAt.Value > DateTime.UtcNow)
                .Select(t => t.PluginId)
                .ToHashSet();
        }

        // Set IsConnected on each DTO
        foreach (var dto in dtos)
        {
            if (!string.IsNullOrEmpty(dto.IntegrationType))
            {
                // No OAuth required = always connected; OAuth required = check token
                dto.IsConnected = noOAuthPlugins.Contains(dto.IntegrationType) ||
                                  connectedOAuthPlugins.Contains(dto.IntegrationType);
            }
        }
    }
}
