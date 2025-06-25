namespace Eventrian.Client.Features.Auth.Interfaces;

public interface ITokenStorageService
{
    /// <summary>
    /// Stores the authentication token in either localStorage or sessionStorage,
    /// depending on whether the user selected "Remember me".
    /// </summary>
    /// <param name="token">The JWT access token to store.</param>
    /// <param name="rememberMe">If true, stores in localStorage (persistent); otherwise, sessionStorage (per session).</param>
    Task SetTokenAsync(string token, bool rememberMe);

    /// <summary>
    /// Retrieves the stored authentication token.
    /// Checks sessionStorage first, then localStorage if not found.
    /// </summary>
    /// <returns>The stored JWT token, or null if none found.</returns>
    Task<string?> GetTokenAsync();

    /// <summary>
    /// Removes the token from both sessionStorage and localStorage.
    /// </summary>
    Task RemoveTokenAsync();
}
