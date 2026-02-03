using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Famick.HomeManagement.Core.DTOs.Products;
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
    private readonly IProductsService _productsService;
    private readonly IFileAccessTokenService _tokenService;
    private readonly IFileStorageService _fileStorage;

    public ShoppingListService(
        HomeManagementDbContext context,
        IMapper mapper,
        ILogger<ShoppingListService> logger,
        IStoreIntegrationService storeIntegrationService,
        IPluginLoader pluginLoader,
        IStockService stockService,
        ITodoItemService todoItemService,
        IProductsService productsService,
        IFileAccessTokenService tokenService,
        IFileStorageService fileStorage)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _storeIntegrationService = storeIntegrationService;
        _pluginLoader = pluginLoader;
        _stockService = stockService;
        _todoItemService = todoItemService;
        _productsService = productsService;
        _tokenService = tokenService;
        _fileStorage = fileStorage;
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
                        .ThenInclude(p => p!.QuantityUnitPurchase)
                .Include(sl => sl.Items!)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images);
        }

        var shoppingList = await query.FirstOrDefaultAsync(sl => sl.Id == id, cancellationToken);

        if (shoppingList == null)
            return null;

        var dto = _mapper.Map<ShoppingListDto>(shoppingList);

        // Populate ImageUrl from product images for items that don't already have one
        if (includeItems && dto.Items != null && shoppingList.Items != null)
        {
            var entityLookup = shoppingList.Items.ToDictionary(i => i.Id);
            foreach (var item in dto.Items.Where(i => string.IsNullOrEmpty(i.ImageUrl) && i.ProductId.HasValue))
            {
                if (entityLookup.TryGetValue(item.Id, out var entity) && entity.Product?.Images != null)
                {
                    var primaryImage = entity.Product.Images
                        .OrderByDescending(img => img.IsPrimary)
                        .ThenBy(img => img.SortOrder)
                        .FirstOrDefault();

                    if (primaryImage != null)
                    {
                        // Prefer external URL (OpenFoodFacts, etc.) over local download URL
                        if (!string.IsNullOrEmpty(primaryImage.ExternalUrl))
                        {
                            item.ImageUrl = primaryImage.ExternalUrl;
                        }
                        else
                        {
                            var token = _tokenService.GenerateToken("product-image", primaryImage.Id, primaryImage.TenantId);
                            item.ImageUrl = _fileStorage.GetProductImageUrl(item.ProductId.Value, primaryImage.Id, token);
                        }
                    }
                }
            }
        }

        // Apply custom aisle ordering if items exist
        if (dto.Items != null && dto.Items.Count > 0 && shoppingList.ShoppingLocationId != Guid.Empty)
        {
            dto.Items = await SortItemsByAisleOrderAsync(
                dto.Items,
                shoppingList.ShoppingLocationId,
                cancellationToken);
        }

        return dto;
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

        var totalUnpurchased = shoppingLists.Sum(sl => sl.Items?.Count(i => !i.IsPurchased) ?? 0);

        return new ShoppingListDashboardDto
        {
            StoresSummary = grouped,
            TotalItems = grouped.Sum(g => g.TotalItems),
            UnpurchasedItems = totalUnpurchased,
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

        // Remove all purchased items from the list
        var purchasedItems = await _context.ShoppingListItems
            .Where(i => i.ShoppingListId == listId && i.IsPurchased)
            .ToListAsync(cancellationToken);

        _context.ShoppingListItems.RemoveRange(purchasedItems);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cleared {Count} purchased items from shopping list: {ListId}",
            purchasedItems.Count, listId);
    }

    public async Task<ClearPurchasedPreviewDto> GetClearPurchasedPreviewAsync(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting clear purchased preview for shopping list: {ListId}", listId);

        // Verify list exists
        var list = await _context.ShoppingLists.FindAsync(new object[] { listId }, cancellationToken);
        if (list == null)
        {
            throw new EntityNotFoundException(nameof(ShoppingList), listId);
        }

        // Get all purchased items with product info
        var purchasedItems = await _context.ShoppingListItems
            .Include(i => i.Product)
                .ThenInclude(p => p!.QuantityUnitPurchase)
            .Include(i => i.Product)
                .ThenInclude(p => p!.Location)
            .Where(i => i.ShoppingListId == listId && i.IsPurchased)
            .ToListAsync(cancellationToken);

        var result = new ClearPurchasedPreviewDto();

        foreach (var item in purchasedItems)
        {
            var previewItem = new ClearPurchasedItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? item.ProductName ?? "Unknown",
                Amount = item.Amount,
                QuantityUnitName = item.Product?.QuantityUnitPurchase?.Name,
                TracksBestBeforeDate = item.Product?.TracksBestBeforeDate ?? false,
                DefaultBestBeforeDays = item.Product?.DefaultBestBeforeDays ?? 0,
                DefaultLocationId = item.Product?.LocationId,
                DefaultLocationName = item.Product?.Location?.Name,
                BestBeforeDate = item.BestBeforeDate,
                ImageUrl = item.ImageUrl,
                Barcode = item.Barcode,
                // Pre-fill UI binding properties
                SelectedLocationId = item.Product?.LocationId,
                SelectedBestBeforeDate = item.BestBeforeDate
            };

            // Calculate default best before date if not already set
            if (previewItem.TracksBestBeforeDate && previewItem.SelectedBestBeforeDate == null && previewItem.DefaultBestBeforeDays > 0)
            {
                previewItem.SelectedBestBeforeDate = DateTime.UtcNow.Date.AddDays(previewItem.DefaultBestBeforeDays);
            }

            if (item.ProductId.HasValue)
            {
                result.ItemsWithProducts.Add(previewItem);
            }
            else
            {
                result.ItemsWithoutProducts.Add(previewItem);
            }
        }

        _logger.LogInformation("Found {WithProducts} items with products and {WithoutProducts} items without products",
            result.InventoryItemCount, result.TodoItemCount);

        return result;
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
            PurchasedAt = request.IsPurchased ? DateTime.UtcNow : null,
            // Use provided store info if available
            Aisle = request.Aisle,
            Department = request.Department,
            ExternalProductId = request.ExternalProductId,
            ImageUrl = request.ImageUrl,
            Price = request.Price
        };

        // If store info not provided and lookup is requested, look up product in store
        var hasStoreInfo = !string.IsNullOrEmpty(request.Aisle) ||
                           !string.IsNullOrEmpty(request.Department) ||
                           !string.IsNullOrEmpty(request.ExternalProductId);

        if (!hasStoreInfo && request.LookupInStore && list.ShoppingLocation?.IntegrationType != null)
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
        MarkItemPurchasedRequest? request = null,
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

        // Handle best before date when marking as purchased
        if (item.IsPurchased)
        {
            if (request?.BestBeforeDate != null)
            {
                // Use explicitly provided date
                item.BestBeforeDate = request.BestBeforeDate;
            }
            else if (item.Product?.TracksBestBeforeDate == true && item.Product.DefaultBestBeforeDays > 0)
            {
                // Auto-calculate date based on product settings
                item.BestBeforeDate = DateTime.UtcNow.Date.AddDays(item.Product.DefaultBestBeforeDays);
            }
            // If TracksBestBeforeDate is false or DefaultBestBeforeDays is 0 without explicit date, leave null
        }
        else
        {
            // Clear best before date when unmarking
            item.BestBeforeDate = null;
        }

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
                    item.ImageUrl = productInfo.ImageUrl;
                    item.Barcode = productInfo.Barcode;

                    _logger.LogInformation(
                        "Found store info for item via ExternalProductId {ExternalProductId}: Aisle={Aisle}, Dept={Department}, ImageUrl={ImageUrl}",
                        item.ExternalProductId, item.Aisle, item.Department, item.ImageUrl);
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
                    item.ImageUrl = bestMatch.ImageUrl;
                    item.Barcode = bestMatch.Barcode;

                    _logger.LogInformation(
                        "Found store info for product {ProductId}: Aisle={Aisle}, Dept={Department}, ImageUrl={ImageUrl}",
                        item.ProductId, item.Aisle, item.Department, item.ImageUrl);
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

    /// <summary>
    /// Sorts shopping list items by the store's custom aisle order, or default order if not configured.
    /// </summary>
    private async Task<List<ShoppingListItemDto>> SortItemsByAisleOrderAsync(
        List<ShoppingListItemDto> items,
        Guid shoppingLocationId,
        CancellationToken cancellationToken)
    {
        var location = await _context.ShoppingLocations
            .FirstOrDefaultAsync(sl => sl.Id == shoppingLocationId, cancellationToken);

        if (location?.AisleOrder == null || location.AisleOrder.Count == 0)
        {
            // Default sorting: numeric then alphabetical, unknown last
            return items
                .OrderBy(i => GetDefaultAisleSortKey(i.Aisle))
                .ThenBy(i => i.Department)
                .ThenBy(i => i.ProductName)
                .ToList();
        }

        // Custom sorting based on AisleOrder
        var aisleOrderMap = location.AisleOrder
            .Select((aisle, index) => new { aisle, index })
            .ToDictionary(x => x.aisle, x => x.index);

        return items
            .OrderBy(i => GetCustomAisleSortKey(i.Aisle, aisleOrderMap))
            .ThenBy(i => i.Department)
            .ThenBy(i => i.ProductName)
            .ToList();
    }

    /// <summary>
    /// Returns a sort key for default aisle ordering: numeric first (by value), then named, then unknown.
    /// </summary>
    private static (int priority, int numericValue, string text) GetDefaultAisleSortKey(string? aisle)
    {
        if (string.IsNullOrEmpty(aisle))
            return (2, 0, ""); // Unknown aisles last

        if (int.TryParse(aisle, out int num))
            return (0, num, ""); // Numeric aisles first

        return (1, 0, aisle); // Named aisles second
    }

    /// <summary>
    /// Returns a sort key for custom aisle ordering based on the configured order map.
    /// </summary>
    private static int GetCustomAisleSortKey(string? aisle, Dictionary<string, int> aisleOrderMap)
    {
        if (string.IsNullOrEmpty(aisle))
            return int.MaxValue; // Unknown aisles last

        if (aisleOrderMap.TryGetValue(aisle, out int order))
            return order;

        // Aisles not in custom order go after ordered ones
        return int.MaxValue - 1;
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
                        PurchasedDate = DateTime.UtcNow,
                        BestBeforeDate = item.BestBeforeDate,
                        LocationId = item.LocationId
                    };

                    var stockEntry = await _stockService.AddStockAsync(addStockRequest, cancellationToken);
                    response.StockEntryIds.Add(stockEntry.Id);
                    response.ItemsAddedToStock++;

                    _logger.LogInformation("Added stock entry {StockEntryId} for product {ProductId}",
                        stockEntry.Id, item.ProductId);
                }
                else
                {
                    // Item has no product - create a basic product and a TODO to review it
                    // Get "Kitchen" location (or first available as fallback)
                    var defaultLocation = await _context.Locations
                        .FirstOrDefaultAsync(l => l.Name == "Kitchen", cancellationToken)
                        ?? await _context.Locations
                            .OrderBy(l => l.Name)
                            .FirstOrDefaultAsync(cancellationToken);

                    var defaultQuantityUnit = await _context.QuantityUnits
                        .OrderBy(q => q.Name)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (defaultLocation == null || defaultQuantityUnit == null)
                    {
                        _logger.LogWarning("Cannot create product - no default location or quantity unit exists");
                        response.Errors.Add($"Cannot create product '{item.ProductName}': No default location or quantity unit configured");
                        continue;
                    }

                    // Create the product with basic info - assume all new products track expiration dates
                    var createProductRequest = new CreateProductRequest
                    {
                        Name = item.ProductName ?? "Unknown product",
                        LocationId = defaultLocation.Id,
                        QuantityUnitIdPurchase = defaultQuantityUnit.Id,
                        QuantityUnitIdStock = defaultQuantityUnit.Id,
                        QuantityUnitFactorPurchaseToStock = 1.0m,
                        TracksBestBeforeDate = true,
                        IsActive = true
                    };

                    var newProduct = await _productsService.CreateAsync(createProductRequest, cancellationToken);

                    _logger.LogInformation("Created product {ProductId} '{ProductName}' from shopping item",
                        newProduct.Id, newProduct.Name);

                    // Add barcode if available
                    if (!string.IsNullOrEmpty(item.Barcode))
                    {
                        await _productsService.AddBarcodeAsync(newProduct.Id, item.Barcode, "Added from shopping", cancellationToken);
                        _logger.LogInformation("Added barcode {Barcode} to product {ProductId}", item.Barcode, newProduct.Id);
                    }

                    // Add image if available (download from URL)
                    if (!string.IsNullOrEmpty(item.ImageUrl))
                    {
                        var imageResult = await _productsService.AddImageFromUrlAsync(newProduct.Id, item.ImageUrl, cancellationToken);
                        if (imageResult != null)
                        {
                            _logger.LogInformation("Added image from URL to product {ProductId}", newProduct.Id);
                        }
                    }

                    // Add to inventory
                    var addStockRequest = new AddStockRequest
                    {
                        ProductId = newProduct.Id,
                        Amount = item.Amount,
                        Price = item.Price,
                        PurchasedDate = DateTime.UtcNow,
                        BestBeforeDate = item.BestBeforeDate,
                        LocationId = item.LocationId ?? defaultLocation.Id
                    };

                    var stockEntry = await _stockService.AddStockAsync(addStockRequest, cancellationToken);
                    response.StockEntryIds.Add(stockEntry.Id);
                    response.ItemsAddedToStock++;

                    _logger.LogInformation("Added stock entry {StockEntryId} for new product {ProductId}",
                        stockEntry.Id, newProduct.Id);

                    // Create a TODO item to review/complete the product setup
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
                        TaskType = TaskType.Product,
                        RelatedEntityId = newProduct.Id,
                        Reason = "New product from shopping - review and complete setup",
                        Description = newProduct.Name,
                        AdditionalData = additionalData
                    };

                    var todoItem = await _todoItemService.CreateAsync(todoRequest, cancellationToken);
                    response.TodoItemIds.Add(todoItem.Id);
                    response.TodoItemsCreated++;

                    _logger.LogInformation("Created TODO item {TodoItemId} for product {ProductId} '{ProductName}'",
                        todoItem.Id, newProduct.Id, newProduct.Name);
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
