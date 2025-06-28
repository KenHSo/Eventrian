namespace Eventrian.Api.Features.Auth.Interfaces;


public interface IRefreshTokenService
{
    /// <summary>
    /// Checks whether the given refresh token is valid for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>True if the token is valid and not expired; otherwise, false.</returns>
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);

    /// <summary>
    /// Rotates the user's refresh token by generating a new one and removing all existing tokens for the user.
    /// </summary>
    /// <remarks>
    /// Enforces a single-token policy: only one valid token is stored per user at a time.
    /// </remarks>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The newly generated refresh token.</returns>
    Task<string> RotateRefreshTokenAsync(string userId);

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

