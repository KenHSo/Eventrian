using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventrian.Api.Features.Auth;

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

}

