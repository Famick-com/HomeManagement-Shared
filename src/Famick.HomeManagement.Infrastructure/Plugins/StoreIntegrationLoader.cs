using System.Reflection;
using System.Text.Json;
using Famick.HomeManagement.Core.Interfaces.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Famick.HomeManagement.Infrastructure.Plugins;

/// <summary>
/// Plugin loader for store integration plugins.
/// Reads configuration from plugins/config.json "storeIntegrations" section.
/// Built-in plugins are loaded by ID, external plugins are loaded from DLL files.
/// </summary>
public class StoreIntegrationLoader : IStoreIntegrationLoader
{
    private readonly ILogger<StoreIntegrationLoader> _logger;
    private readonly PluginLoaderOptions _options;
    private readonly Dictionary<string, IStoreIntegrationPlugin> _builtinPlugins;
    private List<IStoreIntegrationPlugin> _plugins = new();
    private List<StoreIntegrationConfigEntry> _configurations = new();

    public StoreIntegrationLoader(
        ILogger<StoreIntegrationLoader> logger,
        IOptions<PluginLoaderOptions> options,
        IEnumerable<IStoreIntegrationPlugin> builtinPlugins)
    {
        _logger = logger;
        _options = options.Value;
        _builtinPlugins = builtinPlugins.ToDictionary(p => p.PluginId, p => p);
    }

    public IReadOnlyList<IStoreIntegrationPlugin> Plugins => _plugins.AsReadOnly();

    public IReadOnlyList<IStoreIntegrationPlugin> GetAvailablePlugins()
    {
        return _plugins
            .Where(p => p.IsAvailable)
            .ToList()
            .AsReadOnly();
    }

    public IStoreIntegrationPlugin? GetPlugin(string pluginId)
    {
        return _plugins.FirstOrDefault(p => p.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<StoreIntegrationConfigEntry> GetPluginConfigurations()
    {
        return _configurations.AsReadOnly();
    }

    public async Task LoadPluginsAsync(CancellationToken ct = default)
    {
        _plugins.Clear();
        _configurations.Clear();

        var configPath = Path.Combine(_options.PluginsPath, "config.json");

        if (!File.Exists(configPath))
        {
            _logger.LogDebug("Plugin configuration file not found at {Path}. No store integration plugins will be loaded.", configPath);
            return;
        }

        try
        {
            var configJson = await File.ReadAllTextAsync(configPath, ct);
            var configDoc = JsonDocument.Parse(configJson);

            if (!configDoc.RootElement.TryGetProperty("storeIntegrations", out var integrationsArray))
            {
                _logger.LogDebug("No 'storeIntegrations' array found in config.json");
                return;
            }

            foreach (var pluginElement in integrationsArray.EnumerateArray())
            {
                var entry = ParsePluginEntry(pluginElement);
                if (entry == null) continue;

                _configurations.Add(entry);

                if (!entry.Enabled)
                {
                    _logger.LogInformation("Store integration plugin {PluginId} is disabled, skipping", entry.Id);
                    continue;
                }

                var plugin = await LoadPluginAsync(entry, ct);
                if (plugin != null)
                {
                    _plugins.Add(plugin);
                    _logger.LogInformation("Loaded store integration plugin {PluginId} ({DisplayName}) v{Version}",
                        plugin.PluginId, plugin.DisplayName, plugin.Version);
                }
            }

            _logger.LogInformation("Loaded {Count} store integration plugins", _plugins.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load store integration plugins from {Path}", configPath);
        }
    }

    private StoreIntegrationConfigEntry? ParsePluginEntry(JsonElement element)
    {
        try
        {
            var entry = new StoreIntegrationConfigEntry
            {
                Id = element.GetProperty("id").GetString() ?? string.Empty,
                Enabled = element.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean(),
                Builtin = element.TryGetProperty("builtin", out var builtin) && builtin.GetBoolean(),
                Assembly = element.TryGetProperty("assembly", out var assembly) ? assembly.GetString() : null,
                DisplayName = element.TryGetProperty("displayName", out var displayName)
                    ? displayName.GetString() ?? string.Empty
                    : string.Empty
            };

            if (element.TryGetProperty("config", out var config))
            {
                entry.Config = config.Clone();
            }

            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse store integration plugin configuration entry");
            return null;
        }
    }

    private async Task<IStoreIntegrationPlugin?> LoadPluginAsync(StoreIntegrationConfigEntry entry, CancellationToken ct)
    {
        try
        {
            IStoreIntegrationPlugin? plugin;

            if (entry.Builtin)
            {
                // Load built-in plugin by ID
                if (!_builtinPlugins.TryGetValue(entry.Id, out plugin))
                {
                    _logger.LogWarning("Built-in store integration plugin {PluginId} not found", entry.Id);
                    return null;
                }
            }
            else
            {
                // Load external plugin from DLL
                if (string.IsNullOrEmpty(entry.Assembly))
                {
                    _logger.LogWarning("External store integration plugin {PluginId} has no assembly path", entry.Id);
                    return null;
                }

                plugin = LoadExternalPlugin(entry);
                if (plugin == null) return null;
            }

            // Initialize the plugin with its configuration
            await plugin.InitAsync(entry.Config, ct);
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load store integration plugin {PluginId}", entry.Id);
            return null;
        }
    }

    private IStoreIntegrationPlugin? LoadExternalPlugin(StoreIntegrationConfigEntry entry)
    {
        var assemblyPath = Path.Combine(_options.PluginsPath, entry.Assembly!);

        if (!File.Exists(assemblyPath))
        {
            _logger.LogWarning("Store integration plugin assembly not found: {Path}", assemblyPath);
            return null;
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IStoreIntegrationPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (pluginType == null)
            {
                _logger.LogWarning("No IStoreIntegrationPlugin implementation found in {Path}", assemblyPath);
                return null;
            }

            var plugin = (IStoreIntegrationPlugin?)Activator.CreateInstance(pluginType);
            if (plugin == null)
            {
                _logger.LogWarning("Failed to create instance of store integration plugin type {Type}", pluginType.FullName);
                return null;
            }

            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load external store integration plugin from {Path}", assemblyPath);
            return null;
        }
    }
}
