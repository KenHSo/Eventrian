namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Handles forced termination of the user's session, including cleanup and navigation.
/// Used when the system (not the user) triggers logout, such as token expiry or refresh failure.
/// </summary>
public interface IUserSessionTerminator
{
    /// <summary>
    /// Forcefully terminates the current user session by calling <c>LogoutAsync</c> and navigating to the login page.
    /// Used when authentication is no longer valid (e.g., token expired or refresh failed).
    /// This performs full cleanup: tokens, timers, state, and broadcast handling.
    /// </summary>
    /// <param name="fromBroadcast">
    /// If true, the logout was triggered by another tab via broadcast and should not rebroadcast again.
    /// Prevents infinite logout loops across tabs.
    /// </param>
    Task TerminateUserSessionAsync(bool fromBroadcast = false);
}
