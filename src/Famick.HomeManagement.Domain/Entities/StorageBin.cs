namespace Famick.HomeManagement.Domain.Entities;

/// <summary>
/// Represents a labeled storage bin/container with QR code identification.
/// Contains a markdown description of contents and optional photos.
/// </summary>
public class StorageBin : BaseTenantEntity
{
    /// <summary>
    /// Human-readable short code used in QR code URLs (e.g., "blue-oak-47").
    /// Format: adjective-noun-number. Unique within tenant.
    /// </summary>
    public string ShortCode { get; set; } = string.Empty;

    /// <summary>
    /// Markdown description of the bin contents.
    /// Supports basic formatting: bold, italic, lists, headers.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    #region Navigation Properties

    /// <summary>
    /// Photos attached to this storage bin (images of contents).
    /// </summary>
    public virtual ICollection<StorageBinPhoto> Photos { get; set; } = new List<StorageBinPhoto>();

    #endregion
}
