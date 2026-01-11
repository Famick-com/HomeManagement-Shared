using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ProductCommonNames;
using Famick.HomeManagement.Core.DTOs.ProductGroups;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public class ProductCommonNameService : IProductCommonNameService
{
    private readonly HomeManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductCommonNameService> _logger;

    public ProductCommonNameService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<ProductCommonNameService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProductCommonNameDto> CreateAsync(
        CreateProductCommonNameRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating product common name: {Name}", request.Name);

        // Check for duplicate name within tenant
        var exists = await _context.ProductCommonNames
            .AnyAsync(pcn => pcn.Name == request.Name, cancellationToken);

        if (exists)
        {
            throw new DuplicateEntityException(nameof(ProductCommonName), "Name", request.Name);
        }

        var productCommonName = _mapper.Map<ProductCommonName>(request);
        productCommonName.Id = Guid.NewGuid();

        _context.ProductCommonNames.Add(productCommonName);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created product common name: {Id} - {Name}", productCommonName.Id, productCommonName.Name);

        return _mapper.Map<ProductCommonNameDto>(productCommonName);
    }

    public async Task<ProductCommonNameDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var productCommonName = await _context.ProductCommonNames
            .Include(pcn => pcn.Products)
            .FirstOrDefaultAsync(pcn => pcn.Id == id, cancellationToken);

        return productCommonName != null ? _mapper.Map<ProductCommonNameDto>(productCommonName) : null;
    }

    public async Task<List<ProductCommonNameDto>> ListAsync(
        ProductCommonNameFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductCommonNames
            .Include(pcn => pcn.Products)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(pcn =>
                pcn.Name.ToLower().Contains(searchTerm) ||
                (pcn.Description != null && pcn.Description.ToLower().Contains(searchTerm)));
        }

        // Apply sorting
        query = (filter?.SortBy?.ToLower()) switch
        {
            "name" => filter.Descending
                ? query.OrderByDescending(pcn => pcn.Name)
                : query.OrderBy(pcn => pcn.Name),
            "createdat" => filter.Descending
                ? query.OrderByDescending(pcn => pcn.CreatedAt)
                : query.OrderBy(pcn => pcn.CreatedAt),
            "productcount" => filter.Descending
                ? query.OrderByDescending(pcn => pcn.Products!.Count)
                : query.OrderBy(pcn => pcn.Products!.Count),
            _ => query.OrderBy(pcn => pcn.Name) // Default sort by name
        };

        var productCommonNames = await query.ToListAsync(cancellationToken);

        return _mapper.Map<List<ProductCommonNameDto>>(productCommonNames);
    }

    public async Task<ProductCommonNameDto> UpdateAsync(
        Guid id,
        UpdateProductCommonNameRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating product common name: {Id}", id);

        var productCommonName = await _context.ProductCommonNames.FindAsync(new object[] { id }, cancellationToken);
        if (productCommonName == null)
        {
            throw new EntityNotFoundException(nameof(ProductCommonName), id);
        }

        // Check for duplicate name (excluding current entity)
        var exists = await _context.ProductCommonNames
            .AnyAsync(pcn => pcn.Name == request.Name && pcn.Id != id, cancellationToken);

        if (exists)
        {
            throw new DuplicateEntityException(nameof(ProductCommonName), "Name", request.Name);
        }

        _mapper.Map(request, productCommonName);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated product common name: {Id} - {Name}", id, request.Name);

        // Reload with products for DTO mapping
        productCommonName = await _context.ProductCommonNames
            .Include(pcn => pcn.Products)
            .FirstAsync(pcn => pcn.Id == id, cancellationToken);

        return _mapper.Map<ProductCommonNameDto>(productCommonName);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting product common name: {Id}", id);

        var productCommonName = await _context.ProductCommonNames.FindAsync(new object[] { id }, cancellationToken);
        if (productCommonName == null)
        {
            throw new EntityNotFoundException(nameof(ProductCommonName), id);
        }

        _context.ProductCommonNames.Remove(productCommonName);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted product common name: {Id}", id);
    }

    public async Task<List<ProductSummaryDto>> GetProductsWithCommonNameAsync(
        Guid commonNameId,
        CancellationToken cancellationToken = default)
    {
        var productCommonName = await _context.ProductCommonNames.FindAsync(new object[] { commonNameId }, cancellationToken);
        if (productCommonName == null)
        {
            throw new EntityNotFoundException(nameof(ProductCommonName), commonNameId);
        }

        var products = await _context.Products
            .Include(p => p.ProductCommonName)
            .Include(p => p.ShoppingLocation)
            .Where(p => p.ProductCommonNameId == commonNameId)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<ProductSummaryDto>>(products);
    }
}
