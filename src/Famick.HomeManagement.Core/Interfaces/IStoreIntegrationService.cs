using Famick.HomeManagement.Core.DTOs.StoreIntegrations;
using Famick.HomeManagement.Core.Interfaces.Plugins;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for managing store integrations
/// </summary>
public interface IStoreIntegrationService
{
    #region Plugin Discovery

    /// <summary>
    /// Get all available store integration plugins
    /// </summary>
    Task<List<StoreIntegrationPluginInfo>> GetAvailablePluginsAsync(CancellationToken ct = default);

    #endregion

    #region OAuth Flow

    /// <summary>
    /// Get the OAuth authorization URL for a plugin
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="shoppingLocationId">Shopping location to link</param>
    /// <param name="redirectUri">OAuth redirect URI</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Authorization URL to redirect user to</returns>
    Task<string> GetAuthorizationUrlAsync(
        string pluginId,
        Guid shoppingLocationId,
        string redirectUri,
        CancellationToken ct = default);

    /// <summary>
    /// Complete the OAuth flow by exchanging the authorization code for tokens
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <param name="shoppingLocationId">Shopping location to link</param>
    /// <param name="code">Authorization code from OAuth callback</param>
    /// <param name="redirectUri">OAuth redirect URI</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> CompleteOAuthFlowAsync(
        string pluginId,
        Guid shoppingLocationId,
        string code,
        string redirectUri,
        CancellationToken ct = default);

    /// <summary>
    /// Refresh the OAuth token for a shopping location if needed
    /// </summary>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if token is valid (refreshed or still valid)</returns>
    Task<bool> RefreshTokenIfNeededAsync(Guid shoppingLocationId, CancellationToken ct = default);

    /// <summary>
    /// Disconnect a store integration (remove OAuth tokens)
    /// </summary>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    Task DisconnectIntegrationAsync(Guid shoppingLocationId, CancellationToken ct = default);

    #endregion

    #region Store Location

    /// <summary>
    /// Search for store locations
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching store locations</returns>
    Task<List<StoreLocationResult>> SearchStoresAsync(
        StoreSearchRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Link a shopping location to an external store
    /// </summary>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="request">Store link details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> LinkStoreLocationAsync(
        Guid shoppingLocationId,
        LinkStoreLocationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Unlink a shopping location from its external store
    /// </summary>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    Task UnlinkStoreLocationAsync(Guid shoppingLocationId, CancellationToken ct = default);

    #endregion

    #region Product Operations

    /// <summary>
    /// Search for products at a linked store
    /// </summary>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="request">Search parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of matching products</returns>
    Task<List<StoreProductResult>> SearchProductsAtStoreAsync(
        Guid shoppingLocationId,
        StoreProductSearchRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Link a product to a store product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="request">Store product details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created/updated metadata</returns>
    Task<ProductStoreMetadataDto?> LinkProductToStoreAsync(
        Guid productId,
        Guid shoppingLocationId,
        LinkProductToStoreRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get product-store metadata
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Metadata or null if not linked</returns>
    Task<ProductStoreMetadataDto?> GetProductStoreMetadataAsync(
        Guid productId,
        Guid shoppingLocationId,
        CancellationToken ct = default);

    /// <summary>
    /// Get all store metadata for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of metadata for all linked stores</returns>
    Task<List<ProductStoreMetadataDto>> GetAllProductMetadataAsync(
        Guid productId,
        CancellationToken ct = default);

    /// <summary>
    /// Get all product metadata for a store
    /// </summary>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of metadata for all linked products</returns>
    Task<List<ProductStoreMetadataDto>> GetAllStoreMetadataAsync(
        Guid shoppingLocationId,
        CancellationToken ct = default);

    /// <summary>
    /// Refresh product price/availability from the store API
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated metadata</returns>
    Task<ProductStoreMetadataDto?> RefreshProductMetadataAsync(
        Guid productId,
        Guid shoppingLocationId,
        CancellationToken ct = default);

    /// <summary>
    /// Unlink a product from a store
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="shoppingLocationId">Shopping location ID</param>
    /// <param name="ct">Cancellation token</param>
    Task UnlinkProductFromStoreAsync(
        Guid productId,
        Guid shoppingLocationId,
        CancellationToken ct = default);

    #endregion
}
