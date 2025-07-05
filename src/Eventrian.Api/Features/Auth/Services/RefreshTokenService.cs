using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Results;
using Microsoft.EntityFrameworkCore;

namespace Eventrian.Api.Features.Auth.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(AppDbContext db, ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> IssueRefreshTokenAsync(string userId, bool isPersistent)
    {
        var now = DateTime.UtcNow;

        var newToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = now,
            ExpiresAt = now.AddDays(isPersistent ? 7 : 0.5),
            IsPersistent = isPersistent
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

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

        var token = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.ExpiresAt > now);

        return token?.UserId;
    }

    public async Task RevokeRefreshTokensAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (token is not null)
        {
            _db.RefreshTokens.Remove(token);
            await _db.SaveChangesAsync();
        }
    }

    // --- Helpers ---

    /// <summary>
    /// Determines if a token was used again outside the allowed short buffer (5 seconds),
    /// indicating a potential replay attack.
    /// </summary>
    /// <param name="token">The refresh token to evaluate.</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <returns><c>true</c> if replay conditions are met; otherwise, <c>false</c>.</returns>
    private async Task<bool> IsReplayAttackAsync(string token, DateTime now)
    {
        return await _db.RefreshTokens
            .AnyAsync(t => t.Token == token && t.UsedAt != null && t.UsedAt <= now.AddSeconds(-5));
    }

    /// <summary>
    /// Handles a detected replay attack by revoking all tokens for the user linked to the given token.
    /// </summary>
    /// <param name="token">The token that triggered the detection.</param>
    /// <returns>A failed <see cref="RefreshTokenValidationResult"/> instance.</returns>
    private async Task<RefreshTokenValidationResult> HandleReplayAttackAsync(string token)
    {
        var userId = await GetUserIdForToken(token);
        if (userId != null)
        {          
            var all = _db.RefreshTokens.Where(t => t.UserId == userId);
            _db.RefreshTokens.RemoveRange(all);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Replay attack detected - Revoked all tokens for user {UserId}.", userId);
        }

        return RefreshTokenValidationResult.Failure();
    }

    /// <summary>
    /// Retrieves a valid refresh token that hasn't expired or been used outside the short buffer period.
    /// </summary>
    /// <param name="token">The token string to search for.</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <returns>The matching <see cref="RefreshToken"/> entity, or <c>null</c> if not valid.</returns>
    private async Task<RefreshToken?> GetValidRefreshTokenAsync(string token, DateTime now)
    {
        return await _db.RefreshTokens.FirstOrDefaultAsync(r =>
            r.Token == token &&
            r.ExpiresAt > now &&
            (r.UsedAt == null || r.UsedAt > now.AddSeconds(-5)));
    }

    /// <summary>
    /// Marks the token as used if it's older than the overlap window (2 minutes),
    /// indicating it's no longer safe to reuse during rotation.
    /// </summary>
    /// <param name="token">The token entity to update.</param>
    /// <param name="now">The current UTC timestamp.</param>
    private void MarkTokenIfOutsideOverlap(RefreshToken token, DateTime now)
    {
        var withinOverlap = now - token.CreatedAt < TimeSpan.FromMinutes(2);

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
    /// Rotates a refresh token by issuing a new one based on the old token's properties.
    /// </summary>
    /// <param name="oldToken">The token being rotated (exchanged).</param>
    /// <param name="now">The current UTC timestamp.</param>
    /// <returns>The newly created <see cref="RefreshToken"/> entity.</returns>
    private async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken oldToken, DateTime now)
    {
        var newToken = new RefreshToken
        {
            UserId = oldToken.UserId,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = now,
            IsPersistent = oldToken.IsPersistent,
            ExpiresAt = now.AddDays(oldToken.IsPersistent ? 7 : 0.5)
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Rotated refresh token for user {UserId}.", oldToken.UserId);
        return newToken;
    }







    // --- Dev Tasks ---

    // TODO: In production MOVE this to a recurring background job or scheduled task
    // TEMP: Clean up all expired or used tokens
    public async Task RunStartupCleanupAsync()
    {
        var now = DateTime.UtcNow;
        var overlapWindow = TimeSpan.FromMinutes(2);

        var tokensToRemove = await _db.RefreshTokens
            .Where(t =>
                t.ExpiresAt <= now ||
                t.UsedAt != null && t.UsedAt <= now - overlapWindow)
            .ToListAsync();

        if (tokensToRemove.Count > 0)
        {
            _db.RefreshTokens.RemoveRange(tokensToRemove);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Startup cleanup: removed {Count} expired/used tokens.", tokensToRemove.Count);
        }
    }
    // TODO: In production MOVE this to a recurring background job or scheduled task
    // TEMP: Enforces token limit (10 per user) in DB to prevent flooding local DB in testing
    public async Task RunDevTokenCapCleanupAsync(int maxTokensPerUser = 10)
    {
        var userIds = await _db.RefreshTokens
            .Where(t => t.UserId != null)
            .Select(t => t.UserId!)
            .Distinct()
            .ToListAsync();

        foreach (var userId in userIds)
        {
            var tokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip(maxTokensPerUser)
                .ToListAsync();

            if (tokens.Count > 0)
            {
                _db.RefreshTokens.RemoveRange(tokens);
                _logger.LogInformation("Dev cleanup: removed {Count} excess tokens for user {UserId}", tokens.Count, userId);
            }
        }
        await _db.SaveChangesAsync();
    }
}
