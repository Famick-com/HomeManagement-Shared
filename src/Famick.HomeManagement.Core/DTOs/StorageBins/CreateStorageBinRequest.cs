namespace Famick.HomeManagement.Core.DTOs.StorageBins;

/// <summary>
/// Request to create a new storage bin
/// </summary>
public class CreateStorageBinRequest
{
    /// <summary>
    /// Markdown description of the bin contents
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
