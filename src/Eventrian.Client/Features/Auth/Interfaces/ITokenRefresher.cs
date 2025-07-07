namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Handles the logic for refreshing expired or soon-to-expire access tokens.
/// </summary>
public interface ITokenRefresher
{
    /// <summary>
    /// Initializes the refresher by attempting to refresh on app start and setting up the background timer.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Attempts to refresh the access token using the stored refresh token.
    /// </summary>
    /// <returns>True if the token was refreshed successfully; otherwise, false.</returns>
    Task<bool> TryRefreshTokenAsync();

    /// <summary>
    /// Checks the access token expiry and refreshes it if it's near expiration.
    /// </summary>
    Task CheckAndRefreshTokenAsync();

    /// <summary>
    /// Starts the background timer that periodically checks and refreshes the token.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the background token refresh timer.
    /// </summary>
    void Stop();
}
