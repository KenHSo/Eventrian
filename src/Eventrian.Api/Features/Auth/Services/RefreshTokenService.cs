using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Results;
using Microsoft.EntityFrameworkCore;

namespace Eventrian.Api.Features.Auth.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IRefreshTokenRepository _repo;
    private readonly ILogger<RefreshTokenService> _logger;

    private static readonly TimeSpan ReplayAttackWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan OverlapWindow = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PersistentLifespan = TimeSpan.FromDays(7);
    private static readonly TimeSpan SessionLifespan = TimeSpan.FromHours(12);


    public RefreshTokenService(IRefreshTokenRepository repo, ILogger<RefreshTokenService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<string> IssueRefreshTokenAsync(string userId, bool isPersistent)
    {
        var now = DateTime.UtcNow;

        var newToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            IsPersistent = isPersistent,
            CreatedAt = now,
            ExpiresAt = now.Add(isPersistent ? PersistentLifespan : SessionLifespan),
        };

        _repo.Add(newToken);
        await _repo.SaveChangesAsync();

        return newToken.Token;
    }

    public async Task<RefreshTokenValidationResult> ValidateAndRotateAsync(string refreshToken)
    {
        var now = DateTime.UtcNow;

        _logger.LogInformation("Validating refresh token...");

        if (await IsReplayAttackAsync(refreshToken, now))
            return await HandleReplayAttackAsync(refreshToken);

        var token = await GetValidRefreshTokenAsync(refreshToken, now);
        if (token == null)
        {
            _logger.LogWarning("Token validation failed: Token not found, expired, or reused too quickly.");
            return RefreshTokenValidationResult.Failure();
        }

        MarkTokenIfOutsideOverlap(token, now);
        var newToken = await RotateRefreshTokenAsync(token, now);

        return RefreshTokenValidationResult.Success(token.UserId, newToken.Token, token.IsPersistent);
    }

    public async Task<string?> GetUserIdForToken(string refreshToken)
    {
        var now = DateTime.UtcNow;
        return await _repo.GetUserIdForTokenAsync(refreshToken, now);
    }

    public async Task RevokeRefreshTokensAsync(string refreshToken)
    {
        var token = await _repo.FindByTokenAsync(refreshToken);
        if (token is not null)
        {
            _repo.Remove(token);
            await _repo.SaveChangesAsync();
        }
    }


    // --- Helpers ---

    /// <summary>
    /// Checks if the given token was already used and the reuse falls outside the allowed short replay window.
    /// </summary>
    /// <param name="token">The refresh token to evaluate.</param>
    /// <param name="now">The current UTC time for comparison.</param>
    /// <returns>True if a replay attack is suspected; otherwise, false.</returns>
    private async Task<bool> IsReplayAttackAsync(string token, DateTime now)
    {
        var refreshToken = await _repo.FindByTokenAsync(token);
        
        if (refreshToken?.UsedAt == null)
            return false;

        return refreshToken.UsedAt <= now - ReplayAttackWindow;
    }

    /// <summary>
    /// Handles a suspected replay attack by revoking all refresh tokens for the user associated with the given token.
    /// </summary>
    /// <param name="token">The refresh token that triggered the detection.</param>
    /// <returns>A failed <see cref="RefreshTokenValidationResult"/>.</returns>
    private async Task<RefreshTokenValidationResult> HandleReplayAttackAsync(string token)
    {
        var userId = await GetUserIdForToken(token);
        if (userId != null)
        {
            var all = await _repo.GetExcessTokensAsync(userId, 0); // ReplayAttack - So set maxtokens = 0 (get all and remove them) 
            _repo.RemoveRange(all);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Replay attack detected - Revoked all tokens for user {UserId}.", userId);
        }

        return RefreshTokenValidationResult.Failure();
    }

    /// <summary>
    /// Retrieves a refresh token that is not expired and was not used outside the allowed replay window.
    /// </summary>
    /// <param name="token">The refresh token string to validate.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The valid <see cref="RefreshToken"/> if eligible for reuse or rotation; otherwise, null.</returns>
    private async Task<RefreshToken?> GetValidRefreshTokenAsync(string token, DateTime now)
    {
        var result = await _repo.FindByTokenAsync(token);
        if (result == null) return null;

        var usedOutsideReplayWindow = result.UsedAt != null && result.UsedAt <= now - ReplayAttackWindow;
        var expired = result.ExpiresAt <= now;

        return (usedOutsideReplayWindow || expired) ? null : result;
    }

    /// <summary>
    /// Marks a refresh token as used if it falls outside the allowed overlap window for safe reuse.
    /// </summary>
    /// <param name="token">The refresh token entity.</param>
    /// <param name="now">The current UTC time.</param>
    private void MarkTokenIfOutsideOverlap(RefreshToken token, DateTime now)
    {
        var withinOverlap = now - token.CreatedAt < OverlapWindow;

        if (!withinOverlap)
        {
            token.UsedAt = now;
            _logger.LogInformation("Token exceeded overlap window. Marked as used.");
        }
        else
        {
            _logger.LogInformation("Token is within overlap window — keeping temporarily valid.");
        }
    }

    /// <summary>
    /// Issues a new refresh token based on the properties of a previously valid one.
    /// </summary>
    /// <param name="oldToken">The refresh token being rotated.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The newly created <see cref="RefreshToken"/>.</returns>
    private async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, DateTime now)
    {
        var newToken = new RefreshToken
        {
            UserId = oldToken.UserId,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = now,
            IsPersistent = oldToken.IsPersistent,
            ExpiresAt = now.Add(oldToken.IsPersistent ? PersistentLifespan : SessionLifespan) // if oldToken was persistent, the new token should be as well
        };

        _repo.Add(newToken);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Rotated refresh token for user {UserId}.", oldToken.UserId);
        return newToken;
    }








    // --- Dev Tasks ---

    // TODO: In production MOVE this to a recurring background job or scheduled task
    // TEMP: Clean up all expired or used tokens
    public async Task RunStartupCleanupAsync()
    {
        var now = DateTime.UtcNow;

        var tokens = await _repo.GetExpiredOrUsedTokensAsync(now, OverlapWindow);
        if (tokens.Count > 0)
        {
            _repo.RemoveRange(tokens);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Startup cleanup: removed {Count} expired/used tokens.", tokens.Count);
        }
    }

    // TODO: In production MOVE this to a recurring background job or scheduled task
    // TEMP: Enforces token limit (10 per user) in DB to prevent flooding local DB in testing
    public async Task RunDevTokenCapCleanupAsync(int maxTokensPerUser = 10)
    {
        var userIds = await _repo.GetAllUserIdsWithTokensAsync();

        foreach (var userId in userIds)
        {
            var tokens = await _repo.GetExcessTokensAsync(userId, maxTokensPerUser);
            if (tokens.Count > 0)
            {
                _repo.RemoveRange(tokens);
                _logger.LogInformation("Dev cleanup: removed {Count} excess tokens for user {UserId}", tokens.Count, userId);
            }
        }

        await _repo.SaveChangesAsync();
    }

}
