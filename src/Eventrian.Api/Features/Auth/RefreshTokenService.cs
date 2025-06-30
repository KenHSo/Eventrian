using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventrian.Api.Features.Auth;

// Future: To support multiple concurrent logins per user (e.g., different devices),
// consider changing from single refresh token to many-per-user.
// For now, enforcing one refresh token per user keeps it simple and safe.
public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(AppDbContext db, ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string> IssueRefreshTokenAsync(string userId)
    {
        var newToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(newToken);

        await CleanupOldTokensAsync(userId, newToken.Token);
        await _db.SaveChangesAsync();

        return newToken.Token;
    }


    // HERE
    public async Task<(bool IsValid, string? NewToken, string? UserId)> ValidateAndRotateAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to validate and rotate refresh token.");

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r =>
                r.Token == refreshToken &&
                r.ExpiresAt > DateTime.UtcNow &&
                (r.UsedAt == null || r.UsedAt > DateTime.UtcNow.AddSeconds(-5)));

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found, expired, or already used recently.");
            return (false, null, null);
        }

        _logger.LogInformation("Refresh token found for user {UserId}. Rotating token...", token.UserId);

        // Graceful reuse if rotated recently (e.g., last 2 minutes)
        if (DateTime.UtcNow - token.CreatedAt < TimeSpan.FromMinutes(2))
        {
            _logger.LogInformation("Token reused. Too soon to rotate.");
            return (true, token.Token, token.UserId);
        }

        _logger.LogInformation("Refresh token found for user {UserId}. Rotating token...", token.UserId);

        token.UsedAt = DateTime.UtcNow; // Mark the old one as used but keep it for grace period

        var newToken = new RefreshToken
        {
            UserId = token.UserId,
            Token = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(newToken);

        await CleanupOldTokensAsync(token.UserId, newToken.Token); // newToken gets passed in to exclude it from cleanup
        await _db.SaveChangesAsync();

        _logger.LogInformation("Refresh token rotated successfully for user {UserId}.", token.UserId);

        return (true, newToken.Token, token.UserId);
    }







    public async Task<string?> GetUserIdForToken(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.ExpiresAt > DateTime.UtcNow);

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

    private async Task CleanupOldTokensAsync(string userId, string excludeToken)
    {
        var now = DateTime.UtcNow;

        // 1. Remove expired tokens
        var expired = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt <= now)
            .ToListAsync();

        // 2. Only keep recent valid tokens: created within last 10 mins OR used within last 2 mins
        var retainable = await _db.RefreshTokens
            .Where(t =>
                t.UserId == userId &&
                t.ExpiresAt > now &&
                (
                    t.Token == excludeToken || // always keep the newly issued token
                    t.CreatedAt >= now.AddMinutes(-10) ||
                    (t.UsedAt != null && t.UsedAt >= now.AddMinutes(-2))
                ))
            .OrderByDescending(t => t.CreatedAt)
            .Take(10) // max 10 including current
            .ToListAsync();

        var retainableIds = retainable.Select(t => t.Token).ToHashSet();

        // 3. Delete everything else (not expired, not recently used, and not in top 10)
        var toDelete = await _db.RefreshTokens
            .Where(t => t.UserId == userId &&
                        t.ExpiresAt > now &&
                        !retainableIds.Contains(t.Token))
            .ToListAsync();

        // 4. Final cleanup: expired + excess
        var allToRemove = expired.Concat(toDelete).ToList();
        if (allToRemove.Any())
        {
            _db.RefreshTokens.RemoveRange(allToRemove);
            _logger.LogInformation("Cleanup: Removed {Count} old tokens for user {UserId}", allToRemove.Count, userId);
        }
    }
}
