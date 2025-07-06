using Eventrian.Api.Features.Auth.Results;

namespace Eventrian.Api.Features.Auth.Interfaces;

/// <summary>
/// Handles refresh token lifecycle operations including issuing, validating, rotating, and revoking tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for the specified user.
    /// </summary>
    /// <param name="userId">ID of the user the token is issued for.</param>
    /// <param name="isPersistent">
    /// Indicates whether the token is long-lived (e.g., stored in localStorage) or short-lived (e.g., sessionStorage).
    /// </param>
    /// <returns>The generated refresh token string.</returns>
    Task<string> IssueRefreshTokenAsync(string userId, bool isPersistent);

    /// <summary>
    /// Validates and rotates a refresh token if it is valid, unexpired, and not reused outside the allowed replay window.
    /// </summary>
    /// <param name="refreshToken">The token to validate and rotate.</param>
    /// <returns>
    /// A <see cref="RefreshTokenValidationResult"/> indicating success or failure,
    /// and containing the new token and user ID if successful.
    /// </returns>
    Task<RefreshTokenValidationResult> ValidateAndRotateAsync(string refreshToken);

    /// <summary>
    /// Retrieves the user ID linked to a valid, unexpired refresh token.
    /// </summary>
    /// <param name="refreshToken">The token to resolve.</param>
    /// <returns>User ID if valid; otherwise, null.</returns>
    Task<string?> GetUserIdForToken(string refreshToken);

    /// <summary>
    /// Revokes a refresh token by removing it from storage.
    /// </summary>
    /// <param name="refreshToken">The token to revoke.</param>
    Task RevokeRefreshTokensAsync(string refreshToken);

}

