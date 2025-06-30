namespace Eventrian.Api.Features.Auth.Interfaces;


public interface IRefreshTokenService
{
    Task<string> IssueRefreshTokenAsync(string userId);

    Task<(bool IsValid, string? NewToken, string? UserId)> ValidateAndRotateAsync(string refreshToken);

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

