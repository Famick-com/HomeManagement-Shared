namespace Famick.HomeManagement.Core.DTOs.Products;

/// <summary>
/// DTO for product image information.
/// </summary>
public class ProductImageDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Computed URL for displaying the image.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
