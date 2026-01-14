using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Famick.HomeManagement.Core.DTOs.ShoppingLists;
using Famick.HomeManagement.Core.DTOs.Stock;
using Famick.HomeManagement.Core.DTOs.StoreIntegrations;
using Famick.HomeManagement.Core.DTOs.TodoItems;
using Famick.HomeManagement.Core.Exceptions;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Domain.Enums;
using Famick.HomeManagement.Infrastructure.Data;
using Famick.HomeManagement.Infrastructure.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

public partial class ShoppingListService : IShoppingListService
{
    private readonly HomeManagementDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ShoppingListService> _logger;
    private readonly IStoreIntegrationService _storeIntegrationService;
    private readonly IPluginLoader _pluginLoader;
    private readonly IStockService _stockService;
    private readonly ITodoItemService _todoItemService;

    public ShoppingListService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<ShoppingListService> logger,
        IStoreIntegrationService storeIntegrationService,
        IPluginLoader pluginLoader,
        IStockService stockService,
        ITodoItemService todoItemService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _storeIntegrationService = storeIntegrationService;
        _pluginLoader = pluginLoader;
        _stockService = stockService;
        _todoItemService = todoItemService;
    }

    [GeneratedRegex(@"aisle\s*[-:]?\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex AisleRegex();

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
        var query = _context.ShoppingLists
            .Include(sl => sl.ShoppingLocation)
            .AsQueryable();

        if (includeItems)
        {
            query = query
                .Include(sl => sl.Items!)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.QuantityUnitPurchase);
        }

        var shoppingList = await query.FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);

        return shoppingList != null ? _mapper.Map<ShoppingListDto>(shoppingList) : null;
    }

    public async Task<List<ShoppingListSummaryDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var shoppingLists = await _context.ShoppingLists
            .Include(sl => sl.ShoppingLocation)
            .Include(sl => sl.Items)
            .OrderByDescending(sl => sl.UpdatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<ShoppingListSummaryDto>>(shoppingLists);
    }

    public async Task<List<ShoppingListSummaryDto>> ListByStoreAsync(
        Guid shoppingLocationId,
        CancellationToken cancellationToken = default)
    {
        var shoppingLists = await _context.ShoppingLists
            .Include(sl => sl.ShoppingLocation)
            .Include(sl => sl.Items)
            .Where(sl => sl.ShoppingLocationId == shoppingLocationId)
            .OrderByDescending(sl => sl.UpdatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<ShoppingListSummaryDto>>(shoppingLists);
    }

    public async Task<ShoppingListDashboardDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var shoppingLists = await _context.ShoppingLists
            .Include(sl => sl.ShoppingLocation)
            .Include(sl => sl.Items)
            .OrderBy(sl => sl.ShoppingLocation!.Name)
            .ThenByDescending(sl => sl.UpdatedAt)
            .ToListAsync(cancellationToken);

        var grouped = shoppingLists
            .GroupBy(sl => new
            {
                sl.ShoppingLocationId,
                LocationName = sl.ShoppingLocation?.Name ?? "Unknown",
                HasIntegration = sl.ShoppingLocation != null && !string.IsNullOrEmpty(sl.ShoppingLocation.IntegrationType)
            })
            .Select(g => new StoreShoppingListSummary
            {
                ShoppingLocationId = g.Key.ShoppingLocationId,
                ShoppingLocationName = g.Key.LocationName,
                HasIntegration = g.Key.HasIntegration,
                Lists = _mapper.Map<List<ShoppingListSummaryDto>>(g.ToList()),
                TotalItems = g.Sum(sl => sl.Items?.Count ?? 0)
            })
            .ToList();

        return new ShoppingListDashboardDto
        {
            StoresSummary = grouped,
            TotalItems = grouped.Sum(g => g.TotalItems),
            TotalLists = shoppingLists.Count
        };
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

    // Quick add from Stock Overview or barcode scan
    public async Task<ShoppingListItemDto> QuickAddItemAsync(
        AddToShoppingListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Quick adding item to shopping list: {ListId}", request.ShoppingListId);

        // Verify list exists and get its ShoppingLocation
        var list = await _context.ShoppingLists
            .Include(sl => sl.ShoppingLocation)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId, cancellationToken);

        if (list == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), request.ShoppingListId);
        }

        Guid? productId = request.ProductId;

        // If barcode provided, look up product
        if (!productId.HasValue && !string.IsNullOrWhiteSpace(request.Barcode))
        {
            var productByBarcode = await _context.ProductBarcodes
                .Include(pb => pb.Product)
                .FirstOrDefaultAsync(pb => pb.Barcode == request.Barcode, cancellationToken);

            if (productByBarcode != null)
            {
                productId = productByBarcode.ProductId;
            }
            else
            {
                _logger.LogWarning("Product not found for barcode: {Barcode}", request.Barcode);
                throw new DomainException($"No product found with barcode: {request.Barcode}");
            }
        }

        // If no product found but ProductName provided, allow ad-hoc item
        if (!productId.HasValue && string.IsNullOrWhiteSpace(request.ProductName))
        {
            throw new DomainException("Either ProductId, valid Barcode, or ProductName must be provided");
        }

        // Create the item
        var item = new ShoppingListItem
        {
            Id = Guid.NewGuid(),
            ShoppingListId = request.ShoppingListId,
            ProductId = productId,
            ProductName = request.ProductName,
            Amount = request.Amount,
            Note = request.Note,
            IsPurchased = request.IsPurchased,
            PurchasedAt = request.IsPurchased ? DateTime.UtcNow : null
        };

        // If store has integration and lookup is requested, look up product in store
        if (request.LookupInStore && list.ShoppingLocation?.IntegrationType != null)
        {
            await TryPopulateStoreInfoAsync(item, list.ShoppingLocation, cancellationToken);
        }

        _context.ShoppingListItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Quick added item: {Id} to shopping list: {ListId}", item.Id, request.ShoppingListId);

        // Reload with navigation properties
        item = await _context.ShoppingListItems
            .Include(i => i.Product)
                .ThenInclude(p => p!.QuantityUnitPurchase)
            .FirstAsync(i => i.Id == item.Id, cancellationToken);

        return _mapper.Map<ShoppingListItemDto>(item);
    }

    public async Task<ShoppingListItemDto> LookupItemInStoreAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Looking up item in store: {ItemId}", itemId);

        var item = await _context.ShoppingListItems
            .Include(i => i.ShoppingList)
                .ThenInclude(sl => sl!.ShoppingLocation)
            .Include(i => i.Product)
                .ThenInclude(p => p!.QuantityUnitPurchase)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingListItem), itemId);
        }

        var shoppingLocation = item.ShoppingList?.ShoppingLocation;
        if (shoppingLocation?.IntegrationType == null)
        {
            throw new DomainException("Shopping list is not associated with a store integration");
        }

        await TryPopulateStoreInfoAsync(item, shoppingLocation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ShoppingListItemDto>(item);
    }

    public async Task<SendToCartResult> SendToCartAsync(
        Guid listId,
        SendToCartRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending items to cart for list: {ListId}", listId);

        var list = await _context.ShoppingLists
            .Include(sl => sl.ShoppingLocation)
            .Include(sl => sl.Items!)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(sl => sl.Id == listId, cancellationToken);

        if (list == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), listId);
        }

        var shoppingLocation = list.ShoppingLocation;
        if (shoppingLocation?.IntegrationType == null)
        {
            throw new DomainException("Shopping list is not associated with a store integration");
        }

        // Get the plugin
        var plugin = _pluginLoader.GetPlugin<IStoreIntegrationPlugin>(shoppingLocation.IntegrationType);
        if (plugin == null || !plugin.Capabilities.HasShoppingCart)
        {
            throw new DomainException("Store integration does not support shopping cart");
        }

        // Determine which items to send
        var itemsToSend = list.Items?
            .Where(i => !i.IsPurchased && !string.IsNullOrEmpty(i.ExternalProductId))
            .ToList() ?? new List<ShoppingListItem>();

        if (request.ItemIds.Count > 0)
        {
            itemsToSend = itemsToSend.Where(i => request.ItemIds.Contains(i.Id)).ToList();
        }

        var result = new SendToCartResult
        {
            Success = true,
            Details = new List<SendToCartItemResult>()
        };

        // Get access token
        var accessToken = await _storeIntegrationService.GetAccessTokenAsync(
            shoppingLocation.IntegrationType, cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            return new SendToCartResult
            {
                Success = false,
                ItemsFailed = itemsToSend.Count,
                Details = itemsToSend.Select(i => new SendToCartItemResult
                {
                    ItemId = i.Id,
                    ProductName = i.Product?.Name ?? "Unknown",
                    Success = false,
                    ErrorMessage = "Failed to get access token for store integration"
                }).ToList()
            };
        }

        // Build the cart items list
        var cartItems = itemsToSend.Select(i => new CartItemRequest
        {
            ExternalProductId = i.ExternalProductId!,
            Quantity = (int)Math.Ceiling(i.Amount)
        }).ToList();

        try
        {
            var cartResult = await plugin.AddToCartAsync(
                accessToken,
                shoppingLocation.ExternalLocationId!,
                cartItems,
                cancellationToken);

            if (cartResult != null)
            {
                // All items were sent successfully
                foreach (var item in itemsToSend)
                {
                    result.ItemsSent++;
                    result.Details.Add(new SendToCartItemResult
                    {
                        ItemId = item.Id,
                        ProductName = item.Product?.Name ?? "Unknown",
                        Success = true
                    });
                }
            }
            else
            {
                // Failed to add to cart
                foreach (var item in itemsToSend)
                {
                    result.ItemsFailed++;
                    result.Details.Add(new SendToCartItemResult
                    {
                        ItemId = item.Id,
                        ProductName = item.Product?.Name ?? "Unknown",
                        Success = false,
                        ErrorMessage = "Failed to add to cart"
                    });
                }
                result.Success = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding items to cart");
            foreach (var item in itemsToSend)
            {
                result.ItemsFailed++;
                result.Details.Add(new SendToCartItemResult
                {
                    ItemId = item.Id,
                    ProductName = item.Product?.Name ?? "Unknown",
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
            result.Success = false;
        }

        _logger.LogInformation("Sent {Sent} items to cart, {Failed} failed", result.ItemsSent, result.ItemsFailed);

        return result;
    }

    public async Task<ShoppingListItemDto> TogglePurchasedAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Toggling purchased status for item: {ItemId}", itemId);

        var item = await _context.ShoppingListItems
            .Include(i => i.Product)
                .ThenInclude(p => p!.QuantityUnitPurchase)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingListItem), itemId);
        }

        item.IsPurchased = !item.IsPurchased;
        item.PurchasedAt = item.IsPurchased ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled purchased status for item: {ItemId} to {IsPurchased}",
            itemId, item.IsPurchased);

        return _mapper.Map<ShoppingListItemDto>(item);
    }

    private async Task TryPopulateStoreInfoAsync(
        ShoppingListItem item,
        ShoppingLocation shoppingLocation,
        CancellationToken cancellationToken)
    {
        // Only require store integration, not a local product
        if (shoppingLocation.IntegrationType == null)
        {
            return;
        }

        try
        {
            // First check if we already have metadata for this product/store combo (only if we have a ProductId)
            if (item.ProductId.HasValue)
            {
                var existingMetadata = await _context.Set<ProductStoreMetadata>()
                    .FirstOrDefaultAsync(m =>
                        m.ProductId == item.ProductId &&
                        m.ShoppingLocationId == shoppingLocation.Id,
                        cancellationToken);

                if (existingMetadata != null)
                {
                    item.Aisle = NormalizeAisle(existingMetadata.Aisle);
                    item.Shelf = existingMetadata.Shelf;
                    item.Department = existingMetadata.Department;
                    item.ExternalProductId = existingMetadata.ExternalProductId;
                    return;
                }
            }

            // If item already has an ExternalProductId, use it directly to get updated store info
            if (!string.IsNullOrEmpty(item.ExternalProductId))
            {
                var productInfo = await _storeIntegrationService.GetProductAtStoreAsync(
                    shoppingLocation.Id,
                    item.ExternalProductId,
                    cancellationToken);

                if (productInfo != null)
                {
                    item.Aisle = NormalizeAisle(productInfo.Aisle);
                    item.Shelf = productInfo.Shelf;
                    item.Department = productInfo.Department;

                    _logger.LogInformation(
                        "Found store info for item via ExternalProductId {ExternalProductId}: Aisle={Aisle}, Dept={Department}",
                        item.ExternalProductId, item.Aisle, item.Department);
                    return;
                }
            }

            // Fall back to search by barcode/name only if we have a local product
            if (item.ProductId.HasValue)
            {
                var product = await _context.Products
                    .Include(p => p.Barcodes)
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

                if (product == null) return;

                var searchQuery = product.Barcodes?.FirstOrDefault()?.Barcode ?? product.Name;

                var searchRequest = new StoreProductSearchRequest
                {
                    Query = searchQuery,
                    MaxResults = 5
                };

                var searchResults = await _storeIntegrationService.SearchProductsAtStoreAsync(
                    shoppingLocation.Id,
                    searchRequest,
                    cancellationToken);

                if (searchResults.Count > 0)
                {
                    var bestMatch = searchResults.First();
                    item.Aisle = NormalizeAisle(bestMatch.Aisle);
                    item.Shelf = bestMatch.Shelf;
                    item.Department = bestMatch.Department;
                    item.ExternalProductId = bestMatch.ExternalProductId;

                    _logger.LogInformation(
                        "Found store info for product {ProductId}: Aisle={Aisle}, Dept={Department}",
                        item.ProductId, item.Aisle, item.Department);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to look up store info for item: ProductId={ProductId}, ExternalProductId={ExternalProductId}",
                item.ProductId, item.ExternalProductId);
        }
    }

    private static string? NormalizeAisle(string? rawAisle)
    {
        if (string.IsNullOrWhiteSpace(rawAisle)) return null;

        // If aisle contains "aisle", extract just the number/identifier
        var match = AisleRegex().Match(rawAisle);
        return match.Success ? match.Groups[1].Value : rawAisle;
    }

    public async Task<MoveToInventoryResponse> MoveToInventoryAsync(
        MoveToInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moving {Count} items to inventory from shopping list {ListId}",
            request.Items.Count, request.ShoppingListId);

        var response = new MoveToInventoryResponse();
        var itemIdsToRemove = new List<Guid>();

        foreach (var item in request.Items)
        {
            try
            {
                if (item.ProductId.HasValue)
                {
                    // Item has a product - add to stock
                    var addStockRequest = new AddStockRequest
                    {
                        ProductId = item.ProductId.Value,
                        Amount = item.Amount,
                        Price = item.Price,
                        PurchasedDate = DateTime.UtcNow
                    };

                    var stockEntry = await _stockService.AddStockAsync(addStockRequest, cancellationToken);
                    response.StockEntryIds.Add(stockEntry.Id);
                    response.ItemsAddedToStock++;

                    _logger.LogInformation("Added stock entry {StockEntryId} for product {ProductId}",
                        stockEntry.Id, item.ProductId);
                }
                else
                {
                    // Item has no product - create a TODO item for inventory setup
                    var additionalData = JsonSerializer.Serialize(new
                    {
                        Amount = item.Amount,
                        Price = item.Price,
                        Barcode = item.Barcode,
                        ShoppingListId = request.ShoppingListId,
                        ShoppingListItemId = item.ShoppingListItemId
                    });

                    var todoRequest = new CreateTodoItemRequest
                    {
                        TaskType = TaskType.Inventory,
                        Reason = "New product from shopping - needs setup",
                        Description = item.ProductName ?? "Unknown product",
                        AdditionalData = additionalData
                    };

                    var todoItem = await _todoItemService.CreateAsync(todoRequest, cancellationToken);
                    response.TodoItemIds.Add(todoItem.Id);
                    response.TodoItemsCreated++;

                    _logger.LogInformation("Created TODO item {TodoItemId} for product '{ProductName}'",
                        todoItem.Id, item.ProductName);
                }

                // Mark item for removal from shopping list
                itemIdsToRemove.Add(item.ShoppingListItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing item {ShoppingListItemId}: {ProductName}",
                    item.ShoppingListItemId, item.ProductName);
                response.Errors.Add($"Failed to process '{item.ProductName}': {ex.Message}");
            }
        }

        // Remove processed items from the shopping list
        if (itemIdsToRemove.Count > 0)
        {
            var itemsToRemove = await _context.ShoppingListItems
                .Where(i => itemIdsToRemove.Contains(i.Id))
                .ToListAsync(cancellationToken);

            _context.ShoppingListItems.RemoveRange(itemsToRemove);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed {Count} items from shopping list {ListId}",
                itemsToRemove.Count, request.ShoppingListId);
        }

        _logger.LogInformation("Move to inventory complete: {StockCount} added to stock, {TodoCount} TODO items created, {ErrorCount} errors",
            response.ItemsAddedToStock, response.TodoItemsCreated, response.Errors.Count);

        return response;
    }
}
