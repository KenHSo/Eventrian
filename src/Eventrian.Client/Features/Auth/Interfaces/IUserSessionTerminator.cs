namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Handles forced termination of the user's session, including cleanup and navigation.
/// Used when the system (not the user) triggers logout, such as token expiry or refresh failure.
/// </summary>
public interface IUserSessionTerminator
{
    /// <summary>
    /// Terminates the current user session by clearing authentication state and navigating to the login page.
    /// Typically called when authentication is no longer valid (e.g., failed token refresh).
    /// </summary>
    Task TerminateUserSessionAsync(bool fromBroadcast = false);
}
