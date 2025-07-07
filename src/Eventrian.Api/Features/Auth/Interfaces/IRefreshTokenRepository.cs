using Eventrian.Api.Features.Auth.Models;

namespace Eventrian.Api.Features.Auth.Interfaces;

/// <summary>
/// Repository interface for managing refresh tokens in the data store.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Finds a refresh token entity by its token string.
    /// </summary>
    /// <param name="token">The refresh token string to search for.</param>
    /// <returns>The matching <see cref="RefreshToken"/> or null if not found.</returns>
    Task<RefreshToken?> FindByTokenAsync(string token);

    /// <summary>
    /// Retrieves the user ID associated with a valid and unexpired refresh token.
    /// </summary>
    /// <param name="token">The refresh token to validate.</param>
    /// <param name="now">The current timestamp used for expiration checks.</param>
    /// <returns>The user ID if the token is valid; otherwise, null.</returns>
    Task<string?> GetUserIdForTokenAsync(string token, DateTime now);

    /// <summary>
    /// Returns all tokens that are expired or used before a certain overlap window.
    /// </summary>
    /// <param name="now">The current timestamp.</param>
    /// <param name="overlapWindow">The buffer period to consider for used tokens.</param>
    /// <returns>A list of expired or used <see cref="RefreshToken"/> instances.</returns>
    Task<List<RefreshToken>> GetExpiredOrUsedTokensAsync(DateTime now, TimeSpan overlapWindow);

    /// <summary>
    /// [Dev only] Retrieves all distinct user IDs that currently have refresh tokens.
    /// Used by development-only cleanup tasks.
    /// </summary>
    /// <returns>A list of user ID strings.</returns>
    Task<List<string>> GetAllUserIdsWithTokensAsync();

    /// <summary>
    /// [Dev only] Returns the oldest tokens beyond a maximum allowed token count per user.
    /// Used by development-only cleanup to enforce token cap limits.
    /// </summary>
    /// <param name="userId">The user ID whose tokens to inspect.</param>
    /// <param name="maxTokens">The maximum allowed number of tokens.</param>
    /// <returns>A list of excess <see cref="RefreshToken"/> instances to delete.</returns>
    Task<List<RefreshToken>> GetExcessTokensAsync(string userId, int maxTokens);




    /// <summary>
    /// Adds a new refresh token to the data store.
    /// </summary>
    /// <param name="token">The <see cref="RefreshToken"/> to add.</param>
    void Add(RefreshToken token);

    /// <summary>
    /// Removes the specified refresh token from the data store.
    /// </summary>
    /// <param name="token">The <see cref="RefreshToken"/> to remove.</param>
    void Remove(RefreshToken token);

    /// <summary>
    /// Removes a range of refresh tokens from the data store.
    /// </summary>
    /// <param name="tokens">A collection of <see cref="RefreshToken"/> instances to remove.</param>
    void RemoveRange(IEnumerable<RefreshToken> tokens);

    /// <summary>
    /// Persists any pending changes to the data store.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveChangesAsync();
}
