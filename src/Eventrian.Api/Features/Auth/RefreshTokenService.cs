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

    public async Task RevokeRefreshTokensAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (token is not null)
        {
            _db.RefreshTokens.Remove(token);
            await _db.SaveChangesAsync();
        }
    }

    // TODO: When I get more refresh tokens in DB, one for each tab of the user insted of just one, use this to remove all tokens on logout
    // This is assuming I want to log out ALL tabs of the same user - later I can add option to log out all, or log out the tab (YAGNI)
    //public async Task RevokeRefreshTokensAsync(string refreshToken)
    //{
    //    var token = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == refreshToken);
    //    if (token is not null)
    //    {
    //        var userId = token.UserId;

    //        var tokens = await _db.RefreshTokens
    //            .Where(r => r.UserId == userId)
    //            .ToListAsync();

    //        _db.RefreshTokens.RemoveRange(tokens);
    //        await _db.SaveChangesAsync();
    //    }
    //}


}

