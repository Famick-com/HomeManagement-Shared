using Famick.HomeManagement.Core.DTOs.Authentication;

namespace Famick.HomeManagement.Core.Interfaces;

/// <summary>
/// Service for user authentication, registration, and token management
/// </summary>
public interface IAuthenticationService
{
    // NOTE: RegisterTenantAsync is cloud-specific and should be defined in a cloud-specific interface
    // The types RegisterTenantRequest/Response are defined in homemanagement-cloud, not shared
    // This method has been moved to ICloudAuthenticationService in homemanagement-cloud

    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    /// <param name="request">The login request with credentials</param>
    /// <param name="ipAddress">The IP address of the client making the request</param>
    /// <param name="deviceInfo">Device/User-Agent information for the client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with access and refresh tokens</returns>
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string deviceInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// </summary>
    /// <param name="request">The refresh token request</param>
    /// <param name="ipAddress">The IP address of the client making the request</param>
    /// <param name="deviceInfo">Device/User-Agent information for the client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    Task<RefreshTokenResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string ipAddress,
        string deviceInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token (logout)
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user (logout from all devices)
    /// </summary>
    /// <param name="userId">The ID of the user whose tokens should be revoked</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
