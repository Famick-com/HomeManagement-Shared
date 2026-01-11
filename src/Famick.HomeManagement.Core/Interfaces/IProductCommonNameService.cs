using Famick.HomeManagement.Core.DTOs.ProductCommonNames;
using Famick.HomeManagement.Core.DTOs.ProductGroups;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for managing product common names (generic product names)
/// </summary>
public interface IProductCommonNameService
{
    /// <summary>
    /// Creates a new product common name
    /// </summary>
    Task<ProductCommonNameDto> CreateAsync(
        CreateProductCommonNameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product common name by ID
    /// </summary>
    Task<ProductCommonNameDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all product common names with optional filtering
    /// </summary>
    Task<List<ProductCommonNameDto>> ListAsync(
        ProductCommonNameFilterRequest? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product common name
    /// </summary>
    Task<ProductCommonNameDto> UpdateAsync(
        Guid id,
        UpdateProductCommonNameRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product common name
    /// </summary>
    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products with a specific common name
    /// </summary>
    Task<List<ProductSummaryDto>> GetProductsWithCommonNameAsync(
        Guid commonNameId,
        CancellationToken cancellationToken = default);
}
