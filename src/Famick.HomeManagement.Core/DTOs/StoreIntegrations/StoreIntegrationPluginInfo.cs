namespace Famick.HomeManagement.Core.DTOs.StoreIntegrations;

/// <summary>
/// Information about an available store integration plugin
/// </summary>
public class StoreIntegrationPluginInfo
{
    /// <summary>
    /// Plugin identifier (e.g., "kroger")
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Kroger")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Plugin version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Whether the plugin is properly configured and available
    /// </summary>
    public bool IsAvailable { get; set; }
}
