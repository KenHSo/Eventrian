using Eventrian.Api.Features.Auth.Models;

namespace Eventrian.Api.Features.Auth.Interfaces;

/// <summary>
/// Handles refresh token lifecycle operations including issuing, validating, rotating, and revoking tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Issues a new refresh token for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to issue the token for.</param>
    /// <param name="isPersistent">Whether the token should be long-lived (e.g., stored in localStorage) or short-lived (e.g., sessionStorage).</param>
    /// <returns>The newly generated refresh token string.</returns>
    Task<string> IssueRefreshTokenAsync(string userId, bool isPersistent);

    /// <summary>
    /// Validates the given refresh token and rotates it if valid.
    /// May return the same token if within reuse grace period.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>
    /// A <see cref="TokenValidationResult"/> containing:
    /// <list type="bullet">
    ///   <item><description><c>IsValid</c>: Whether the token was accepted.</description></item>
    ///   <item><description><c>NewRefreshToken</c>: The rotated token if successful, otherwise null.</description></item>
    ///   <item><description><c>UserId</c>: The ID of the user associated with the token, if valid.</description></item>
    ///   <item><description><c>IsPersistent</c>: Whether the token was persistent (used for client storage logic).</description></item>
    /// </list>
    /// </returns>

    Task<TokenValidationResult> ValidateAndRotateAsync(string refreshToken);

    /// <summary>
    /// Retrieves the user ID associated with a valid and unexpired refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to look up.</param>
    /// <returns>The user ID if the token is valid; otherwise, null.</returns>
    Task<string?> GetUserIdForToken(string refreshToken);

    /// <summary>
    /// Revokes the specified refresh token by removing it from the database.
    /// </summary>
    /// <param name="refreshToken">The token to revoke.</param>
    Task RevokeRefreshTokensAsync(string refreshToken);

}

