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

    /// <summary>
    /// Downloads an image from a URL and saves it to product storage.
    /// Used for self-hosted deployments to cache external images locally.
    /// </summary>
    /// <param name="productId">The product ID for organizing storage.</param>
    /// <param name="imageUrl">The URL to download the image from.</param>
    /// <param name="source">The source identifier (e.g., "kroger", "openfoodfacts") for folder organization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored file name, or null if download failed.</returns>
    Task<string?> DownloadAndSaveProductImageAsync(
        Guid productId,
        string imageUrl,
        string source,
        CancellationToken ct = default);

    #region Equipment Documents

    /// <summary>
    /// Saves an equipment document to storage.
    /// </summary>
    /// <param name="equipmentId">The equipment ID for organizing storage.</param>
    /// <param name="stream">The file stream to save.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored file name (unique).</returns>
    Task<string> SaveEquipmentDocumentAsync(Guid equipmentId, Stream stream, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Deletes an equipment document from storage.
    /// </summary>
    /// <param name="equipmentId">The equipment ID.</param>
    /// <param name="fileName">The stored file name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteEquipmentDocumentAsync(Guid equipmentId, string fileName, CancellationToken ct = default);

    /// <summary>
    /// Gets the URL for accessing an equipment document.
    /// </summary>
    /// <param name="equipmentId">The equipment ID.</param>
    /// <param name="fileName">The stored file name.</param>
    /// <returns>The URL to access the document.</returns>
    string GetEquipmentDocumentUrl(Guid equipmentId, string fileName);

    /// <summary>
    /// Deletes all documents for an equipment item (used when deleting equipment).
    /// </summary>
    /// <param name="equipmentId">The equipment ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAllEquipmentDocumentsAsync(Guid equipmentId, CancellationToken ct = default);

    #endregion
}
