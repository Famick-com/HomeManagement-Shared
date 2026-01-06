using Famick.HomeManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// File storage service that saves files to the local file system.
/// Files are stored outside wwwroot in {contentRoot}/uploads/{type}/{id}/{filename}
/// to prevent direct access - all files must be served through authenticated API endpoints.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly HttpClient _httpClient;

    public LocalFileStorageService(
        string contentRootPath,
        string baseUrl,
        ILogger<LocalFileStorageService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _basePath = Path.Combine(contentRootPath, "uploads");
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;
        _httpClient = httpClientFactory?.CreateClient("ImageDownloader") ?? new HttpClient();

        // Ensure base uploads directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveProductImageAsync(Guid productId, Stream stream, string fileName, CancellationToken ct = default)
    {
        var directory = GetProductImageDirectory(productId);
        Directory.CreateDirectory(directory);

        var uniqueFileName = GenerateUniqueFileName(fileName);
        var filePath = Path.Combine(directory, uniqueFileName);

        try
        {
            await using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream, ct);

            _logger.LogInformation("Saved product image {FileName} for product {ProductId}", uniqueFileName, productId);
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save product image {FileName} for product {ProductId}", fileName, productId);
            throw;
        }
    }

    public Task DeleteProductImageAsync(Guid productId, string fileName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(GetProductImageDirectory(productId), fileName);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted product image {FileName} for product {ProductId}", fileName, productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product image {FileName} for product {ProductId}", fileName, productId);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public string GetProductImageUrl(Guid productId, Guid imageId, string? accessToken = null)
    {
        var url = $"{_baseUrl}/api/v1/products/{productId}/images/{imageId}/download";
        return string.IsNullOrEmpty(accessToken) ? url : $"{url}?token={accessToken}";
    }

    public string GetProductImagePath(Guid productId, string fileName)
    {
        return Path.Combine(GetProductImageDirectory(productId), fileName);
    }

    public Task DeleteAllProductImagesAsync(Guid productId, CancellationToken ct = default)
    {
        var directory = GetProductImageDirectory(productId);

        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                _logger.LogInformation("Deleted all images for product {ProductId}", productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image directory for product {ProductId}", productId);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public async Task<string?> DownloadAndSaveProductImageAsync(
        Guid productId,
        string imageUrl,
        string source,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        try
        {
            // Download the image
            using var response = await _httpClient.GetAsync(imageUrl, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image from {Url}: {StatusCode}", imageUrl, response.StatusCode);
                return null;
            }

            // Determine file extension from content type or URL
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var extension = GetExtensionFromContentType(contentType) ?? GetExtensionFromUrl(imageUrl) ?? ".jpg";

            // Generate filename with source prefix
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var random = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"{source}_{timestamp}_{random}{extension}";

            // Save the image
            var directory = GetProductImageDirectory(productId);
            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, fileName);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream, ct);

            _logger.LogInformation("Downloaded and saved product image {FileName} from {Source} for product {ProductId}",
                fileName, source, productId);

            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and save product image from {Url} for product {ProductId}", imageUrl, productId);
            return null;
        }
    }

    private static string? GetExtensionFromContentType(string? contentType)
    {
        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "image/bmp" => ".bmp",
            _ => null
        };
    }

    private static string? GetExtensionFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension) ? null : extension.ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }

    private string GetProductImageDirectory(Guid productId)
    {
        return Path.Combine(_basePath, "products", productId.ToString());
    }

    private static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"image_{timestamp}_{random}{extension}";
    }

    #region Equipment Documents

    public async Task<string> SaveEquipmentDocumentAsync(Guid equipmentId, Stream stream, string fileName, CancellationToken ct = default)
    {
        var directory = GetEquipmentDocumentDirectory(equipmentId);
        Directory.CreateDirectory(directory);

        var uniqueFileName = GenerateUniqueDocumentFileName(fileName);
        var filePath = Path.Combine(directory, uniqueFileName);

        try
        {
            await using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream, ct);

            _logger.LogInformation("Saved equipment document {FileName} for equipment {EquipmentId}", uniqueFileName, equipmentId);
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save equipment document {FileName} for equipment {EquipmentId}", fileName, equipmentId);
            throw;
        }
    }

    public Task DeleteEquipmentDocumentAsync(Guid equipmentId, string fileName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(GetEquipmentDocumentDirectory(equipmentId), fileName);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted equipment document {FileName} for equipment {EquipmentId}", fileName, equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete equipment document {FileName} for equipment {EquipmentId}", fileName, equipmentId);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public string GetEquipmentDocumentUrl(Guid documentId, string? accessToken = null)
    {
        var url = $"{_baseUrl}/api/v1/equipment/documents/{documentId}/download";
        return string.IsNullOrEmpty(accessToken) ? url : $"{url}?token={accessToken}";
    }

    public string GetEquipmentDocumentPath(Guid equipmentId, string fileName)
    {
        return Path.Combine(GetEquipmentDocumentDirectory(equipmentId), fileName);
    }

    public Task DeleteAllEquipmentDocumentsAsync(Guid equipmentId, CancellationToken ct = default)
    {
        var directory = GetEquipmentDocumentDirectory(equipmentId);

        if (Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                _logger.LogInformation("Deleted all documents for equipment {EquipmentId}", equipmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document directory for equipment {EquipmentId}", equipmentId);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    private string GetEquipmentDocumentDirectory(Guid equipmentId)
    {
        return Path.Combine(_basePath, "equipment", equipmentId.ToString());
    }

    private static string GenerateUniqueDocumentFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"doc_{timestamp}_{random}{extension}";
    }

    #endregion
}
