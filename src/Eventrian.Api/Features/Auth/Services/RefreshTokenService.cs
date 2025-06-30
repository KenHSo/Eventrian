using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Models;
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

    public async Task<TokenValidationResult> ValidateAndRotateAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to validate and rotate refresh token.");

        // Check for refresh token reuse outside overlap
        if (await IsRefreshTokenReusedAsync(refreshToken))
        {
            _logger.LogWarning("Detected reuse of refresh token beyond overlap window. Forcing global logout.");

            var userId = await GetUserIdForToken(refreshToken);
            if (userId != null)
            {
                var allTokens = _db.RefreshTokens.Where(t => t.UserId == userId);
                _db.RefreshTokens.RemoveRange(allTokens);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Revoked all tokens for user {UserId} due to reuse.", userId);
            }

            return new TokenValidationResult
            {
                IsValid = false,
                NewRefreshToken = null,
                UserId = null,
                IsPersistent = null
            };
        }

        var now = DateTime.UtcNow;

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r =>
                r.Token == refreshToken &&
                r.ExpiresAt > now &&
                (r.UsedAt == null || r.UsedAt > now.AddSeconds(-5)));

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found, expired, or already used recently.");
            return new TokenValidationResult
            {
                IsValid = false,
                NewRefreshToken = null,
                UserId = null,
                IsPersistent = null
            };
        }

        _logger.LogInformation("Refresh token found for user {UserId}. Rotating token...", token.UserId);

        var withinOverlapWindow = now - token.CreatedAt < TimeSpan.FromMinutes(2);
        if (!withinOverlapWindow)
        {
            // Token is older than the overlap window: mark as used so it can't be reused
            token.UsedAt = now;
            _logger.LogInformation("Token exceeded overlap window. Marking as used.");
        }
        else
        {
            _logger.LogInformation("Token is within overlap window. Keeping it temporarily valid.");
        }

        // Always issue a new refresh token, regardless of overlap window
        var newToken = new RefreshToken
        {
            UserId = token.UserId,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = now,
            IsPersistent = token.IsPersistent,
            ExpiresAt = now.AddDays(token.IsPersistent ? 7 : 0.5)
        };


        _db.RefreshTokens.Add(newToken);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Refresh token rotated successfully for user {UserId}.", token.UserId);

        return new TokenValidationResult
        {
            IsValid = true,
            NewRefreshToken = newToken.Token,
            UserId = token.UserId,
            IsPersistent = token.IsPersistent
        };
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

    private async Task<bool> IsRefreshTokenReusedAsync(string refreshToken)
    {
        var now = DateTime.UtcNow;

        var reusedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(r =>
                r.Token == refreshToken &&
                r.UsedAt != null &&
                r.UsedAt <= now.AddSeconds(-5));

        return reusedToken != null;
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
        var userIds = await _db.RefreshTokens.Select(t => t.UserId).Distinct().ToListAsync();
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
