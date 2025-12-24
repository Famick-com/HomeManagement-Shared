using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ShoppingLists;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public class ShoppingListService : IShoppingListService
{
    private readonly HomeManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ShoppingListService> _logger;

    public ShoppingListService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<ShoppingListService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    // List management
    public async Task<ShoppingListDto> CreateListAsync(
        CreateShoppingListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating shopping list: {Name}", request.Name);

        var shoppingList = _mapper.Map<ShoppingList>(request);
        shoppingList.Id = Guid.NewGuid();

        _context.ShoppingLists.Add(shoppingList);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created shopping list: {Id} - {Name}", shoppingList.Id, shoppingList.Name);

        return _mapper.Map<ShoppingListDto>(shoppingList);
    }

    public async Task<ShoppingListDto?> GetListByIdAsync(
        Guid id,
        bool includeItems = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShoppingLists.AsQueryable();

        if (includeItems)
        {
            query = query
                .Include(sl => sl.Items!)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.ShoppingLocation);
        }

        var shoppingList = await query.FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);

        return shoppingList != null ? _mapper.Map<ShoppingListDto>(shoppingList) : null;
    }

    public async Task<List<ShoppingListSummaryDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var shoppingLists = await _context.ShoppingLists
            .Include(sl => sl.Items)
            .OrderByDescending(sl => sl.UpdatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<ShoppingListSummaryDto>>(shoppingLists);
    }

    public async Task<ShoppingListDto> UpdateListAsync(
        Guid id,
        UpdateShoppingListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating shopping list: {Id}", id);

        var shoppingList = await _context.ShoppingLists.FindAsync(new object[] { id }, cancellationToken);
        if (shoppingList == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), id);
        }

        _mapper.Map(request, shoppingList);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated shopping list: {Id} - {Name}", id, request.Name);

        // Reload with items for DTO mapping
        shoppingList = await _context.ShoppingLists
            .Include(sl => sl.Items!)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ShoppingLocation)
            .FirstAsync(sl => sl.Id == id, cancellationToken);

        return _mapper.Map<ShoppingListDto>(shoppingList);
    }

    public async Task DeleteListAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting shopping list: {Id}", id);

        var shoppingList = await _context.ShoppingLists.FindAsync(new object[] { id }, cancellationToken);
        if (shoppingList == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), id);
        }

        _context.ShoppingLists.Remove(shoppingList);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted shopping list: {Id}", id);
    }

    // Item management
    public async Task<ShoppingListItemDto> AddItemAsync(
        Guid listId,
        AddShoppingListItemRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding item to shopping list: {ListId}", listId);

        // Verify list exists
        var list = await _context.ShoppingLists.FindAsync(new object[] { listId }, cancellationToken);
        if (list == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), listId);
        }

        // Verify product exists if provided
        if (request.ProductId.HasValue)
        {
            var product = await _context.Products.FindAsync(new object[] { request.ProductId.Value }, cancellationToken);
            if (product == null)
            {
                throw new EntityNotFoundException(nameof(Product), request.ProductId.Value);
            }
        }

        var item = _mapper.Map<ShoppingListItem>(request);
        item.Id = Guid.NewGuid();
        item.ShoppingListId = listId;

        _context.ShoppingListItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added item: {Id} to shopping list: {ListId}", item.Id, listId);

        // Reload with navigation properties
        item = await _context.ShoppingListItems
            .Include(i => i.Product)
                .ThenInclude(p => p!.ShoppingLocation)
            .FirstAsync(i => i.Id == item.Id, cancellationToken);

        return _mapper.Map<ShoppingListItemDto>(item);
    }

    public async Task<ShoppingListItemDto> UpdateItemAsync(
        Guid itemId,
        UpdateShoppingListItemRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating shopping list item: {ItemId}", itemId);

        var item = await _context.ShoppingListItems.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingListItem), itemId);
        }

        _mapper.Map(request, item);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated shopping list item: {ItemId}", itemId);

        // Reload with navigation properties
        item = await _context.ShoppingListItems
            .Include(i => i.Product)
                .ThenInclude(p => p!.ShoppingLocation)
            .FirstAsync(i => i.Id == itemId, cancellationToken);

        return _mapper.Map<ShoppingListItemDto>(item);
    }

    public async Task RemoveItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing shopping list item: {ItemId}", itemId);

        var item = await _context.ShoppingListItems.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingListItem), itemId);
        }

        _context.ShoppingListItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed shopping list item: {ItemId}", itemId);
    }

    public async Task MarkItemAsPurchasedAsync(
        Guid itemId,
        MarkItemPurchasedRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Marking item as purchased: {ItemId}", itemId);

        var item = await _context.ShoppingListItems.FindAsync(new object[] { itemId }, cancellationToken);
        if (item == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingListItem), itemId);
        }

        // TODO: Future enhancement - create stock entry when StockService is available
        // For now, just remove the item from the list
        _context.ShoppingListItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked item as purchased (removed): {ItemId}", itemId);
    }

    public async Task ClearPurchasedItemsAsync(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing purchased items from shopping list: {ListId}", listId);

        // Verify list exists
        var list = await _context.ShoppingLists.FindAsync(new object[] { listId }, cancellationToken);
        if (list == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), listId);
        }

        // TODO: Future enhancement - identify "purchased" items
        // For now, this is a no-op since items are removed when marked as purchased
        // In the future, we might have a "Purchased" flag or timestamp

        _logger.LogInformation("Cleared purchased items from shopping list: {ListId}", listId);

        await Task.CompletedTask;
    }

    // Smart features
    public async Task<List<ProductSuggestionDto>> SuggestProductsAsync(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting products for shopping list: {ListId}", listId);

        // Verify list exists
        var list = await _context.ShoppingLists.FindAsync(new object[] { listId }, cancellationToken);
        if (list == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), listId);
        }

        // Get current stock levels
        var stockLevels = await _context.Stock
            .GroupBy(s => s.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                CurrentStock = g.Sum(s => s.Amount)
            })
            .ToListAsync(cancellationToken);

        // Get products with MinStockAmount threshold
        var products = await _context.Products
            .Where(p => p.MinStockAmount > 0)
            .ToListAsync(cancellationToken);

        var suggestions = new List<ProductSuggestionDto>();

        foreach (var product in products)
        {
            var stock = stockLevels.FirstOrDefault(s => s.ProductId == product.Id);
            var currentStock = stock?.CurrentStock ?? 0;

            // Suggest if below minimum
            if (currentStock < product.MinStockAmount)
            {
                var suggestedAmount = product.MinStockAmount - currentStock;
                var reason = currentStock == 0 ? "Out of stock" : "Below minimum";

                suggestions.Add(new ProductSuggestionDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentStock = currentStock,
                    MinStockAmount = product.MinStockAmount,
                    SuggestedAmount = suggestedAmount,
                    Reason = reason
                });
            }
        }

        _logger.LogInformation("Found {Count} product suggestions for shopping list: {ListId}",
            suggestions.Count, listId);

        return suggestions.OrderBy(s => s.CurrentStock).ToList();
    }

    public async Task<ShoppingListByLocationDto> GroupItemsByLocationAsync(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Grouping items by location for shopping list: {ListId}", listId);

        var shoppingList = await _context.ShoppingLists
            .Include(sl => sl.Items!)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ShoppingLocation)
            .FirstOrDefaultAsync(sl => sl.Id == listId, cancellationToken);

        if (shoppingList == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), listId);
        }

        var items = shoppingList.Items ?? new List<ShoppingListItem>();

        // Group items by shopping location
        var grouped = items
            .GroupBy(i => new
            {
                LocationId = i.Product?.ShoppingLocationId,
                LocationName = i.Product?.ShoppingLocation?.Name ?? "Unassigned"
            })
            .Select(g => new LocationItemGroup
            {
                ShoppingLocationId = g.Key.LocationId,
                ShoppingLocationName = g.Key.LocationName,
                Items = _mapper.Map<List<ShoppingListItemDto>>(g.ToList())
            })
            .OrderBy(g => g.ShoppingLocationName)
            .ToList();

        var result = new ShoppingListByLocationDto
        {
            ShoppingListId = listId,
            ShoppingListName = shoppingList.Name,
            ItemsByLocation = grouped
        };

        _logger.LogInformation("Grouped {ItemCount} items into {GroupCount} location groups",
            items.Count, grouped.Count);

        return result;
    }
}
