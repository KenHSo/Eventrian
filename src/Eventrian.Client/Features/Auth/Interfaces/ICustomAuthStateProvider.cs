namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Exposes methods to notify the application of authentication state changes
/// and check token validity.
/// </summary>
public interface ICustomAuthStateProvider
{
    /// <summary>
    /// Notifies the application that the user has successfully authenticated.
    /// Triggers an update to the authentication state.
    /// </summary>
    Task NotifyUserAuthentication();

    /// <summary>
    /// Notifies the application that the user has logged out.
    /// Clears the current authentication state.
    /// </summary>
    void NotifyUserLogout();

    /// <summary>
    /// Determines whether a given JWT access token has expired.
    /// </summary>
    /// <param name="token">The JWT access token to evaluate.</param>
    /// <returns>True if the token is expired; otherwise, false.</returns>
    bool IsTokenExpired(string accessToken);
}

