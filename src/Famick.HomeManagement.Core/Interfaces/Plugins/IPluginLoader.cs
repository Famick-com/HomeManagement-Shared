namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Service for loading and managing product lookup plugins
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// All loaded plugins (both built-in and external)
    /// </summary>
    IReadOnlyList<IProductLookupPlugin> Plugins { get; }

    /// <summary>
    /// Get all enabled and available plugins in config.json order (pipeline execution order)
    /// </summary>
    IReadOnlyList<IProductLookupPlugin> GetAvailablePlugins();

    /// <summary>
    /// Get a specific plugin by ID
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>The plugin, or null if not found</returns>
    IProductLookupPlugin? GetPlugin(string pluginId);

    /// <summary>
    /// Load/reload all plugins from the configuration
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task LoadPluginsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get plugin configuration entries from config.json
    /// </summary>
    IReadOnlyList<PluginConfigEntry> GetPluginConfigurations();
}

/// <summary>
/// Plugin configuration entry from plugins/config.json
/// </summary>
public class PluginConfigEntry
{
    /// <summary>
    /// Plugin identifier
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
    /// Display name for the plugin
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Plugin-specific configuration (as a JsonElement)
    /// </summary>
    public System.Text.Json.JsonElement? Config { get; set; }
}
