using Famick.HomeManagement.Core.DTOs.Home;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for managing home information, utilities, and setup wizard
/// </summary>
public interface IHomeService
{
    /// <summary>
    /// Gets the home for the current tenant. Returns null if no home exists.
    /// </summary>
    Task<HomeDto?> GetHomeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the home setup wizard has been completed
    /// </summary>
    Task<bool> IsHomeSetupCompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes the initial home setup wizard
    /// </summary>
    Task<HomeDto> SetupHomeAsync(
        HomeSetupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the home information
    /// </summary>
    Task<HomeDto> UpdateHomeAsync(
        UpdateHomeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a utility to the home
    /// </summary>
    Task<HomeUtilityDto> AddUtilityAsync(
        CreateHomeUtilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing utility
    /// </summary>
    Task<HomeUtilityDto> UpdateUtilityAsync(
        Guid utilityId,
        UpdateHomeUtilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a utility from the home
    /// </summary>
    Task DeleteUtilityAsync(
        Guid utilityId,
        CancellationToken cancellationToken = default);
}
