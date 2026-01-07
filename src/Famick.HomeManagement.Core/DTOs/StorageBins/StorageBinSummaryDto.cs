namespace Famick.HomeManagement.Core.DTOs.StorageBins;

/// <summary>
/// Lightweight storage bin summary for lists
/// </summary>
public class StorageBinSummaryDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable short code (e.g., "blue-oak-47")
    /// </summary>
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// First line of description for list preview
    /// </summary>
    public string DescriptionPreview { get; set; } = string.Empty;

    /// <summary>
    /// Number of photos attached to this bin
    /// </summary>
    public int PhotoCount { get; set; }

    /// <summary>
    /// When the bin was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
