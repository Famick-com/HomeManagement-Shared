namespace Famick.HomeManagement.Core.DTOs.StorageBins;

/// <summary>
/// Request to update an existing storage bin
/// </summary>
public class UpdateStorageBinRequest
{
    /// <summary>
    /// Markdown description of the bin contents
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
