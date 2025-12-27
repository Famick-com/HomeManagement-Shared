using Famick.HomeManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Famick.HomeManagement.Infrastructure.Services;

/// <summary>
/// File storage service that saves files to the local file system.
/// Files are stored in wwwroot/uploads/products/{productId}/{filename}
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        string webRootPath,
        string baseUrl,
        ILogger<LocalFileStorageService> logger)
    {
        _basePath = Path.Combine(webRootPath, "uploads");
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;

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

    public string GetProductImageUrl(Guid productId, string fileName)
    {
        return $"{_baseUrl}/uploads/products/{productId}/{fileName}";
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
}
