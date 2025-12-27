namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for storing and retrieving files (images, documents, etc.)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a product image to storage.
    /// </summary>
    /// <param name="productId">The product ID for organizing storage.</param>
    /// <param name="stream">The file stream to save.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored file name (unique).</returns>
    Task<string> SaveProductImageAsync(Guid productId, Stream stream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Deletes a product image from storage.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="fileName">The stored file name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteProductImageAsync(Guid productId, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Gets the URL for accessing a product image.
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="fileName">The stored file name.</param>
    /// <returns>The URL to access the image.</returns>
    string GetProductImageUrl(Guid productId, string fileName);

    /// <summary>
    /// Deletes all images for a product (used when deleting a product).
    /// </summary>
    /// <param name="productId">The product ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAllProductImagesAsync(Guid productId, CancellationToken ct = default);
}
