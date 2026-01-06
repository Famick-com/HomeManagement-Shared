using Famick.HomeManagement.Core.DTOs.Products;

namespace Famick.HomeManagement.Core.Interfaces;

public interface IProductsService
{
    // Product CRUD
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> ListAsync(ProductFilterRequest? filter = null, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Barcode management
    Task<ProductBarcodeDto> AddBarcodeAsync(Guid productId, string barcode, string? note = null, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task DeleteBarcodeAsync(Guid barcodeId, CancellationToken cancellationToken = default);

    // Image management
    Task<ProductImageDto> AddImageAsync(Guid productId, Stream imageStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<List<ProductImageDto>> GetImagesAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductImageDto?> GetImageByIdAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task ReorderImagesAsync(Guid productId, List<Guid> imageIds, CancellationToken cancellationToken = default);

    // Stock level indicators (Phase 2)
    Task<List<ProductStockLevelDto>> GetStockLevelsAsync(ProductFilterRequest? filter = null, CancellationToken cancellationToken = default);
    Task<List<ProductDto>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

    // Search enhancement (Phase 2)
    Task<List<ProductDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
