using Famick.HomeManagement.Core.DTOs.ShoppingLists;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for managing shopping lists and their items
/// </summary>
public interface IShoppingListService
{
    // List management
    Task<ShoppingListDto> CreateListAsync(
        CreateShoppingListRequest request,
        CancellationToken cancellationToken = default);

    Task<ShoppingListDto?> GetListByIdAsync(
        Guid id,
        bool includeItems = true,
        CancellationToken cancellationToken = default);

    Task<List<ShoppingListSummaryDto>> ListAllAsync(
        CancellationToken cancellationToken = default);

    Task<List<ShoppingListSummaryDto>> ListByStoreAsync(
        Guid shoppingLocationId,
        CancellationToken cancellationToken = default);

    Task<ShoppingListDashboardDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<ShoppingListDto> UpdateListAsync(
        Guid id,
        UpdateShoppingListRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteListAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Item management
    Task<ShoppingListItemDto> AddItemAsync(
        Guid listId,
        AddShoppingListItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ShoppingListItemDto> UpdateItemAsync(
        Guid itemId,
        UpdateShoppingListItemRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task MarkItemAsPurchasedAsync(
        Guid itemId,
        MarkItemPurchasedRequest request,
        CancellationToken cancellationToken = default);

    Task ClearPurchasedItemsAsync(
        Guid listId,
        CancellationToken cancellationToken = default);

    // Quick add from Stock Overview or barcode scan
    Task<ShoppingListItemDto> QuickAddItemAsync(
        AddToShoppingListRequest request,
        CancellationToken cancellationToken = default);

    // Store integration
    Task<ShoppingListItemDto> LookupItemInStoreAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<SendToCartResult> SendToCartAsync(
        Guid listId,
        SendToCartRequest request,
        CancellationToken cancellationToken = default);

    // Toggle purchased status
    Task<ShoppingListItemDto> TogglePurchasedAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    // Smart features
    Task<List<ProductSuggestionDto>> SuggestProductsAsync(
        Guid listId,
        CancellationToken cancellationToken = default);

    Task<ShoppingListByLocationDto> GroupItemsByLocationAsync(
        Guid listId,
        CancellationToken cancellationToken = default);

    // Move to inventory
    Task<MoveToInventoryResponse> MoveToInventoryAsync(
        MoveToInventoryRequest request,
        CancellationToken cancellationToken = default);
}
