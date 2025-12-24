using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ProductGroups;
using Famick.HomeManagement.Core.DTOs.ShoppingLocations;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
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

    public ShoppingLocationService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<ShoppingLocationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
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

        return _mapper.Map<ShoppingLocationDto>(shoppingLocation);
    }

    public async Task<ShoppingLocationDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var shoppingLocation = await _context.ShoppingLocations
            .Include(sl => sl.Products)
            .FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);

        return shoppingLocation != null ? _mapper.Map<ShoppingLocationDto>(shoppingLocation) : null;
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

        return _mapper.Map<List<ShoppingLocationDto>>(shoppingLocations);
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

        return _mapper.Map<ShoppingLocationDto>(shoppingLocation);
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
}
