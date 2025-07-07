namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Provides an abstraction for storing and retrieving refresh tokens using browser storage.
/// Handles logic for session vs. persistent storage based on user preferences.
/// </summary>
public interface IRefreshTokenStorage
{
    /// <summary>
    /// Stores the refresh token in either localStorage or sessionStorage,
    /// depending on whether the user selected "Remember me".
    /// </summary>
    /// <param name="token">The JWT access token to store.</param>
    /// <param name="rememberMe">If true, stores in localStorage (persistent); otherwise, sessionStorage (per session).</param>
    Task SetRefreshTokenAsync(string token, bool rememberMe);

    /// <summary>
    /// Retrieves the stored refresh token.
    /// Checks sessionStorage first, then localStorage if not found.
    /// </summary>
    /// <returns>The stored refresh token, or null if none found.</returns>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Removes the refresh token from both sessionStorage and localStorage.
    /// </summary>
    Task RemoveRefreshTokenAsync();

    /// <summary>
    /// Returns true if the refresh token is currently stored in localStorage.
    /// </summary>
    Task<bool> HasLocalStorageTokenAsync();

    Task<bool> IsRefreshInProgressAsync();

    Task SetRefreshInProgressAsync(bool isInProgress);
}
