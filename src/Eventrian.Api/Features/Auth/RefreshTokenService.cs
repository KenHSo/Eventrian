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

    public RefreshTokenService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken);

        return token != null && token.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<string> RotateRefreshTokenAsync(string userId)
    {
        // Remove old tokens (cleanup)
        var oldTokens = _db.RefreshTokens.Where(rt => rt.UserId == userId);
        _db.RefreshTokens.RemoveRange(oldTokens);

        var newToken = new RefreshToken
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString()
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();

        return newToken.Token;
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
}

