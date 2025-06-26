namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Exposes methods to notify the application of authentication state changes.
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

}

