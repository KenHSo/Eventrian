namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Provides access to the in-memory access token.
/// </summary>
public interface IAccessTokenStorage
{
    /// <summary>
    /// Returns the currently stored access token, or null if none is set.
    /// </summary>
    string? GetAccessToken();

    /// <summary>
    /// Stores the access token in memory.
    /// </summary>
    void SetAccessToken(string token);

    /// <summary>
    /// Clears the stored access token from memory.
    /// </summary>
    void ClearAccessToken();

    /// <summary>
    /// Prevents updates to the access token (e.g., during logout).
    /// </summary>
    void BlockTokenUpdates();

    /// <summary>
    /// Allows updates to the access token (e.g., during login or app start).
    /// </summary>
    void AllowTokenUpdates();

    /// <summary>
    /// Indicates whether token updates are currently blocked.
    /// </summary>
    bool TokenUpdatesBlocked { get; }
}
