using Famick.HomeManagement.Core.DTOs.StoreIntegrations;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// Service for managing store integrations.
/// OAuth tokens are now shared per tenant/plugin in TenantIntegrationToken table.
/// </summary>
public class StoreIntegrationService : IStoreIntegrationService
{
    private readonly HomeManagementDbContext _dbContext;
    private readonly IStoreIntegrationLoader _pluginLoader;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<StoreIntegrationService> _logger;

    public StoreIntegrationService(
        HomeManagementDbContext dbContext,
        IStoreIntegrationLoader pluginLoader,
        ITenantProvider tenantProvider,
        ILogger<StoreIntegrationService> logger)
    {
        _dbContext = dbContext;
        _pluginLoader = pluginLoader;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    #region Plugin Discovery

    public async Task<List<StoreIntegrationPluginInfo>> GetAvailablePluginsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId;

        var plugins = new List<StoreIntegrationPluginInfo>();

        foreach (var plugin in _pluginLoader.Plugins)
        {
            var info = new StoreIntegrationPluginInfo
            {
                PluginId = plugin.PluginId,
                DisplayName = plugin.DisplayName,
                Version = plugin.Version,
                IsAvailable = plugin.IsAvailable,
                Capabilities = plugin.Capabilities
            };

            // Plugins that don't require OAuth are always "connected"
            if (!plugin.Capabilities.RequiresOAuth)
            {
                info.IsConnected = true;
            }
            else if (tenantId.HasValue)
            {
                // Check if we have a valid token for this plugin
                var token = await _dbContext.TenantIntegrationTokens
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == plugin.PluginId, ct);

                if (token != null)
                {
                    info.IsConnected = !string.IsNullOrEmpty(token.AccessToken) &&
                                       !token.RequiresReauth &&
                                       token.ExpiresAt.HasValue &&
                                       token.ExpiresAt.Value > DateTime.UtcNow;
                    info.RequiresReauth = token.RequiresReauth;
                }
            }

            plugins.Add(info);
        }

        return plugins;
    }

    #endregion

    #region OAuth Flow

    public Task<string> GetAuthorizationUrlAsync(
        string pluginId,
        Guid shoppingLocationId,
        string redirectUri,
        CancellationToken ct = default)
    {
        var plugin = GetPluginOrThrow(pluginId);

        // Create state parameter with shopping location ID
        // In production, this should be encrypted/signed to prevent tampering
        var state = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{shoppingLocationId}|{Guid.NewGuid()}"));

        var authUrl = plugin.GetAuthorizationUrl(redirectUri, state);

        _logger.LogInformation(
            "Generated OAuth authorization URL for plugin {PluginId}, shopping location {ShoppingLocationId}",
            pluginId, shoppingLocationId);

        return Task.FromResult(authUrl);
    }

    public async Task<bool> CompleteOAuthFlowAsync(
        string pluginId,
        Guid shoppingLocationId,
        string code,
        string redirectUri,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId
            ?? throw new InvalidOperationException("Tenant ID not available");

        var plugin = GetPluginOrThrow(pluginId);

        var tokenResult = await plugin.ExchangeCodeForTokenAsync(code, redirectUri, ct);

        if (!tokenResult.Success)
        {
            _logger.LogWarning(
                "OAuth token exchange failed for plugin {PluginId}: {Error}",
                pluginId, tokenResult.ErrorMessage);
            return false;
        }

        // Store tokens in TenantIntegrationToken (shared across all stores for this tenant/plugin)
        var existingToken = await _dbContext.TenantIntegrationTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PluginId == pluginId, ct);

        if (existingToken != null)
        {
            // Update existing token
            existingToken.AccessToken = tokenResult.AccessToken;
            existingToken.RefreshToken = tokenResult.RefreshToken ?? existingToken.RefreshToken;
            existingToken.ExpiresAt = tokenResult.ExpiresAt;
            existingToken.LastRefreshedAt = DateTime.UtcNow;
            existingToken.RequiresReauth = false;
        }
        else
        {
            // Create new token
            var newToken = new TenantIntegrationToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PluginId = pluginId,
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                LastRefreshedAt = DateTime.UtcNow,
                RequiresReauth = false
            };
            _dbContext.TenantIntegrationTokens.Add(newToken);
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "OAuth flow completed for plugin {PluginId}, shopping location {ShoppingLocationId}",
            pluginId, shoppingLocationId);

        return true;
    }

    public async Task<string?> GetAccessTokenAsync(string pluginId, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
            return null;

        var token = await _dbContext.TenantIntegrationTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == pluginId, ct);

        if (token == null || token.RequiresReauth)
            return null;

        // Check if token needs refresh (5 minute buffer)
        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5))
        {
            if (!await RefreshSharedTokenAsync(pluginId, ct))
            {
                return null;
            }
            // Re-fetch updated token
            token = await _dbContext.TenantIntegrationTokens
                .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == pluginId, ct);
        }

        return token?.AccessToken;
    }

    public async Task<bool> IsConnectedAsync(string pluginId, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
            return false;

        var token = await _dbContext.TenantIntegrationTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == pluginId, ct);

        if (token == null || token.RequiresReauth)
            return false;

        // Token exists and doesn't require re-auth
        // We consider it "connected" even if expired (auto-refresh will handle it)
        return !string.IsNullOrEmpty(token.AccessToken) || !string.IsNullOrEmpty(token.RefreshToken);
    }

    public async Task<bool> RequiresReauthAsync(string pluginId, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
            return false;

        var token = await _dbContext.TenantIntegrationTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == pluginId, ct);

        return token?.RequiresReauth ?? false;
    }

    public async Task<bool> RefreshTokenIfNeededAsync(Guid shoppingLocationId, CancellationToken ct = default)
    {
        var location = await _dbContext.ShoppingLocations
            .FirstOrDefaultAsync(sl => sl.Id == shoppingLocationId, ct);

        if (location == null || string.IsNullOrEmpty(location.IntegrationType))
        {
            return false;
        }

        return await RefreshSharedTokenAsync(location.IntegrationType, ct);
    }

    private async Task<bool> RefreshSharedTokenAsync(string pluginId, CancellationToken ct)
    {
        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
            return false;

        var token = await _dbContext.TenantIntegrationTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == pluginId, ct);

        if (token == null)
        {
            _logger.LogWarning("No token found for plugin {PluginId}", pluginId);
            return false;
        }

        // Check if token is still valid (with 5 minute buffer)
        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value > DateTime.UtcNow.AddMinutes(5))
        {
            return true; // Token is still valid
        }

        if (string.IsNullOrEmpty(token.RefreshToken))
        {
            _logger.LogWarning("Cannot refresh token for plugin {PluginId}: no refresh token", pluginId);
            token.RequiresReauth = true;
            await _dbContext.SaveChangesAsync(ct);
            return false;
        }

        var plugin = _pluginLoader.GetPlugin(pluginId);
        if (plugin == null)
        {
            _logger.LogWarning("Plugin {PluginId} not found", pluginId);
            return false;
        }

        var tokenResult = await plugin.RefreshTokenAsync(token.RefreshToken, ct);

        if (!tokenResult.Success)
        {
            _logger.LogWarning(
                "Token refresh failed for plugin {PluginId}: {Error}. Re-authentication required.",
                pluginId, tokenResult.ErrorMessage);

            // Mark as requiring re-auth instead of failing completely
            token.RequiresReauth = true;
            await _dbContext.SaveChangesAsync(ct);
            return false;
        }

        // Update token
        token.AccessToken = tokenResult.AccessToken;
        if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
        {
            token.RefreshToken = tokenResult.RefreshToken;
        }
        token.ExpiresAt = tokenResult.ExpiresAt;
        token.LastRefreshedAt = DateTime.UtcNow;
        token.RequiresReauth = false;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Token refreshed for plugin {PluginId}", pluginId);
        return true;
    }

    public async Task DisconnectPluginAsync(string pluginId, CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId;
        if (!tenantId.HasValue)
            return;

        var token = await _dbContext.TenantIntegrationTokens
            .FirstOrDefaultAsync(t => t.TenantId == tenantId.Value && t.PluginId == pluginId, ct);

        if (token != null)
        {
            _dbContext.TenantIntegrationTokens.Remove(token);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Disconnected plugin {PluginId} for tenant {TenantId}", pluginId, tenantId);
        }
    }

    [Obsolete("Use DisconnectPluginAsync to remove OAuth tokens, or UnlinkStoreLocationAsync to unlink a store")]
    public async Task DisconnectIntegrationAsync(Guid shoppingLocationId, CancellationToken ct = default)
    {
        // For backward compatibility, just unlink the store
        // OAuth tokens are now managed at the plugin level
        await UnlinkStoreLocationAsync(shoppingLocationId, ct);
    }

    #endregion

    #region Store Location

    public async Task<List<StoreLocationResult>> SearchStoresAsync(
        StoreSearchRequest request,
        CancellationToken ct = default)
    {
        var plugin = GetPluginOrThrow(request.PluginId);

        if (!string.IsNullOrEmpty(request.ZipCode))
        {
            return await plugin.SearchStoresByZipAsync(request.ZipCode, request.RadiusMiles, ct);
        }

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            return await plugin.SearchStoresByCoordinatesAsync(
                request.Latitude.Value, request.Longitude.Value, request.RadiusMiles, ct);
        }

        throw new ArgumentException("Either ZipCode or Latitude/Longitude must be provided");
    }

    public async Task<bool> LinkStoreLocationAsync(
        Guid shoppingLocationId,
        LinkStoreLocationRequest request,
        CancellationToken ct = default)
    {
        var location = await _dbContext.ShoppingLocations
            .FirstOrDefaultAsync(sl => sl.Id == shoppingLocationId, ct);

        if (location == null)
        {
            _logger.LogWarning("Shopping location {ShoppingLocationId} not found", shoppingLocationId);
            return false;
        }

        location.IntegrationType = request.PluginId;
        location.ExternalLocationId = request.ExternalLocationId;
        location.ExternalChainId = request.ExternalChainId;
        location.StoreAddress = request.StoreAddress;
        location.StorePhone = request.StorePhone;
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;

        // Optionally update the name if provided
        if (!string.IsNullOrEmpty(request.StoreName))
        {
            location.Name = request.StoreName;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Linked shopping location {ShoppingLocationId} to external store {ExternalLocationId}",
            shoppingLocationId, request.ExternalLocationId);

        return true;
    }

    public async Task UnlinkStoreLocationAsync(Guid shoppingLocationId, CancellationToken ct = default)
    {
        var location = await _dbContext.ShoppingLocations
            .FirstOrDefaultAsync(sl => sl.Id == shoppingLocationId, ct);

        if (location == null) return;

        location.IntegrationType = null;
        location.ExternalLocationId = null;
        location.ExternalChainId = null;
        // Keep address/phone/coordinates as they might be useful
        // OAuth tokens are now stored in TenantIntegrationToken, not here

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Unlinked shopping location {ShoppingLocationId}", shoppingLocationId);
    }

    #endregion

    #region Product Operations

    public async Task<List<StoreProductResult>> SearchProductsAtStoreAsync(
        Guid shoppingLocationId,
        StoreProductSearchRequest request,
        CancellationToken ct = default)
    {
        var location = await _dbContext.ShoppingLocations
            .FirstOrDefaultAsync(sl => sl.Id == shoppingLocationId, ct);

        if (location == null)
            throw new InvalidOperationException($"Shopping location {shoppingLocationId} not found");

        if (string.IsNullOrEmpty(location.IntegrationType))
            throw new InvalidOperationException("Shopping location has no store integration");

        if (string.IsNullOrEmpty(location.ExternalLocationId))
            throw new InvalidOperationException("Shopping location has no external location ID");

        // Get access token (auto-refreshes if needed)
        var accessToken = await GetAccessTokenAsync(location.IntegrationType, ct);
        if (accessToken == null)
            throw new InvalidOperationException("Unable to authenticate with store. Please reconnect the integration.");

        var plugin = GetPluginOrThrow(location.IntegrationType);

        return await plugin.SearchProductsAsync(
            accessToken,
            location.ExternalLocationId,
            request.Query,
            request.MaxResults,
            ct);
    }

    public async Task<ProductStoreMetadataDto?> LinkProductToStoreAsync(
        Guid productId,
        Guid shoppingLocationId,
        LinkProductToStoreRequest request,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.TenantId
            ?? throw new InvalidOperationException("Tenant ID not available");

        // Check if link already exists
        var existing = await _dbContext.ProductStoreMetadata
            .FirstOrDefaultAsync(psm =>
                psm.ProductId == productId &&
                psm.ShoppingLocationId == shoppingLocationId, ct);

        if (existing != null)
        {
            // Update existing
            existing.ExternalProductId = request.ExternalProductId;
            existing.LastKnownPrice = request.Price;
            existing.PriceUnit = request.PriceUnit;
            existing.PriceUpdatedAt = DateTime.UtcNow;
            existing.Aisle = request.Aisle;
            existing.Shelf = request.Shelf;
            existing.Department = request.Department;
            existing.InStock = request.InStock;
            existing.AvailabilityCheckedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            existing = new ProductStoreMetadata
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = productId,
                ShoppingLocationId = shoppingLocationId,
                ExternalProductId = request.ExternalProductId,
                LastKnownPrice = request.Price,
                PriceUnit = request.PriceUnit,
                PriceUpdatedAt = DateTime.UtcNow,
                Aisle = request.Aisle,
                Shelf = request.Shelf,
                Department = request.Department,
                InStock = request.InStock,
                AvailabilityCheckedAt = DateTime.UtcNow
            };
            _dbContext.ProductStoreMetadata.Add(existing);
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Linked product {ProductId} to store {ShoppingLocationId} with external ID {ExternalProductId}",
            productId, shoppingLocationId, request.ExternalProductId);

        return await GetProductStoreMetadataAsync(productId, shoppingLocationId, ct);
    }

    public async Task<ProductStoreMetadataDto?> GetProductStoreMetadataAsync(
        Guid productId,
        Guid shoppingLocationId,
        CancellationToken ct = default)
    {
        var metadata = await _dbContext.ProductStoreMetadata
            .Include(psm => psm.Product)
            .Include(psm => psm.ShoppingLocation)
            .FirstOrDefaultAsync(psm =>
                psm.ProductId == productId &&
                psm.ShoppingLocationId == shoppingLocationId, ct);

        return metadata == null ? null : MapToDto(metadata);
    }

    public async Task<List<ProductStoreMetadataDto>> GetAllProductMetadataAsync(
        Guid productId,
        CancellationToken ct = default)
    {
        var metadata = await _dbContext.ProductStoreMetadata
            .Include(psm => psm.Product)
            .Include(psm => psm.ShoppingLocation)
            .Where(psm => psm.ProductId == productId)
            .ToListAsync(ct);

        return metadata.Select(MapToDto).ToList();
    }

    public async Task<List<ProductStoreMetadataDto>> GetAllStoreMetadataAsync(
        Guid shoppingLocationId,
        CancellationToken ct = default)
    {
        var metadata = await _dbContext.ProductStoreMetadata
            .Include(psm => psm.Product)
            .Include(psm => psm.ShoppingLocation)
            .Where(psm => psm.ShoppingLocationId == shoppingLocationId)
            .ToListAsync(ct);

        return metadata.Select(MapToDto).ToList();
    }

    public async Task<ProductStoreMetadataDto?> RefreshProductMetadataAsync(
        Guid productId,
        Guid shoppingLocationId,
        CancellationToken ct = default)
    {
        var metadata = await _dbContext.ProductStoreMetadata
            .Include(psm => psm.ShoppingLocation)
            .FirstOrDefaultAsync(psm =>
                psm.ProductId == productId &&
                psm.ShoppingLocationId == shoppingLocationId, ct);

        if (metadata == null || string.IsNullOrEmpty(metadata.ExternalProductId))
            return null;

        var location = metadata.ShoppingLocation;
        if (string.IsNullOrEmpty(location.IntegrationType) ||
            string.IsNullOrEmpty(location.ExternalLocationId))
            return null;

        // Get access token (auto-refreshes if needed)
        var accessToken = await GetAccessTokenAsync(location.IntegrationType, ct);
        if (accessToken == null)
            return null;

        var plugin = _pluginLoader.GetPlugin(location.IntegrationType);
        if (plugin == null) return null;

        var storeProduct = await plugin.GetProductAsync(
            accessToken,
            location.ExternalLocationId,
            metadata.ExternalProductId,
            ct);

        if (storeProduct == null) return null;

        // Update metadata
        metadata.LastKnownPrice = storeProduct.Price;
        metadata.PriceUnit = storeProduct.PriceUnit;
        metadata.PriceUpdatedAt = DateTime.UtcNow;
        metadata.Aisle = storeProduct.Aisle;
        metadata.Shelf = storeProduct.Shelf;
        metadata.Department = storeProduct.Department;
        metadata.InStock = storeProduct.InStock;
        metadata.AvailabilityCheckedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Refreshed product metadata for product {ProductId} at store {ShoppingLocationId}",
            productId, shoppingLocationId);

        return await GetProductStoreMetadataAsync(productId, shoppingLocationId, ct);
    }

    public async Task UnlinkProductFromStoreAsync(
        Guid productId,
        Guid shoppingLocationId,
        CancellationToken ct = default)
    {
        var metadata = await _dbContext.ProductStoreMetadata
            .FirstOrDefaultAsync(psm =>
                psm.ProductId == productId &&
                psm.ShoppingLocationId == shoppingLocationId, ct);

        if (metadata != null)
        {
            _dbContext.ProductStoreMetadata.Remove(metadata);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Unlinked product {ProductId} from store {ShoppingLocationId}",
                productId, shoppingLocationId);
        }
    }

    #endregion

    #region Helpers

    private IStoreIntegrationPlugin GetPluginOrThrow(string pluginId)
    {
        var plugin = _pluginLoader.GetPlugin(pluginId);
        if (plugin == null)
            throw new InvalidOperationException($"Store integration plugin '{pluginId}' not found");
        if (!plugin.IsAvailable)
            throw new InvalidOperationException($"Store integration plugin '{pluginId}' is not available");
        return plugin;
    }

    private static ProductStoreMetadataDto MapToDto(ProductStoreMetadata metadata)
    {
        return new ProductStoreMetadataDto
        {
            Id = metadata.Id,
            ProductId = metadata.ProductId,
            ShoppingLocationId = metadata.ShoppingLocationId,
            ProductName = metadata.Product?.Name,
            ShoppingLocationName = metadata.ShoppingLocation?.Name,
            ExternalProductId = metadata.ExternalProductId,
            LastKnownPrice = metadata.LastKnownPrice,
            PriceUnit = metadata.PriceUnit,
            PriceUpdatedAt = metadata.PriceUpdatedAt,
            Aisle = metadata.Aisle,
            Shelf = metadata.Shelf,
            Department = metadata.Department,
            InStock = metadata.InStock,
            AvailabilityCheckedAt = metadata.AvailabilityCheckedAt,
            CreatedAt = metadata.CreatedAt,
            UpdatedAt = metadata.UpdatedAt
        };
    }

    #endregion
}
