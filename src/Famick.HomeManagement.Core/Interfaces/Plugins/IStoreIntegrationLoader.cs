namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Service for loading and managing store integration plugins
/// </summary>
public interface IStoreIntegrationLoader
{
    /// <summary>
    /// All loaded store integration plugins (both built-in and external)
    /// </summary>
    IReadOnlyList<IStoreIntegrationPlugin> Plugins { get; }

    /// <summary>
    /// Get all enabled and available store integration plugins
    /// </summary>
    IReadOnlyList<IStoreIntegrationPlugin> GetAvailablePlugins();

    /// <summary>
    /// Get a specific plugin by ID
    /// </summary>
    /// <param name="pluginId">Plugin identifier (e.g., "kroger")</param>
    /// <returns>The plugin, or null if not found</returns>
    IStoreIntegrationPlugin? GetPlugin(string pluginId);

    /// <summary>
    /// Load/reload all store integration plugins from the configuration
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task LoadPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get store integration plugin configuration entries from config.json
    /// </summary>
    IReadOnlyList<StoreIntegrationConfigEntry> GetPluginConfigurations();
}

/// <summary>
/// Store integration plugin configuration entry from plugins/config.json
/// </summary>
public class StoreIntegrationConfigEntry
{
    /// <summary>
    /// Plugin identifier (e.g., "kroger")
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Whether this plugin is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether this is a built-in plugin (compiled into the application)
    /// </summary>
    public bool Builtin { get; set; }

    /// <summary>
    /// Path to the DLL file for external plugins (relative to plugins folder)
    /// </summary>
    public string? Assembly { get; set; }

    /// <summary>
    /// Display name for the plugin (e.g., "Kroger")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Plugin-specific configuration (as a JsonElement)
    /// Typically includes clientId, clientSecret, scope, etc.
    /// </summary>
    public System.Text.Json.JsonElement? Config { get; set; }
}
