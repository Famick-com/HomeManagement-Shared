using Famick.HomeManagement.Core.DTOs.Common;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for normalizing and geocoding addresses via external APIs
/// </summary>
public interface IAddressNormalizationService
{
    /// <summary>
    /// Normalizes and geocodes an address
    /// </summary>
    /// <param name="request">The address to normalize</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Normalized address with coordinates, or null if not found</returns>
    Task<NormalizedAddressResult?> NormalizeAsync(
        NormalizeAddressRequest request,
        CancellationToken cancellationToken = default);
}
