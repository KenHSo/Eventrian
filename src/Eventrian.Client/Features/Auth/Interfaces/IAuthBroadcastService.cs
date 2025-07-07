namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Handles cross-tab logout synchronization using BroadcastChannel via JS interop.
/// Responsible for initializing per-tab identity, broadcasting logout events,
/// and notifying when a logout event for the current user is received.
/// </summary>
public interface IAuthBroadcastService
{
    /// <summary>
    /// Initializes the broadcast listener and sets the current user's ID in JS.
    /// Should be called after login to enable logout sync for this session.
    /// </summary>
    /// <param name="userId">The unique identifier of the currently logged-in user.</param>
    Task InitLogoutBroadcastAsync(string userId);

    /// <summary>
    /// Sends a logout event via BroadcastChannel to notify other tabs
    /// that the current user has logged out.
    /// </summary>
    /// <param name="userId">The user ID whose session was logged out.</param>
    Task BroadcastLogoutAsync(string userId);

    /// <summary>
    /// Clears the current user's ID in JS to stop listening for logout broadcasts.
    /// Should be called during logout to clean up JS memory state.
    /// </summary>
    Task ClearUserAsync();

    /// <summary>
    /// Raised when a logout broadcast is received for the currently logged-in user.
    /// Other services (like UserSessionTerminator) should subscribe to this to trigger logout.
    /// </summary>
    event Action? OnLogoutBroadcasted;
}
