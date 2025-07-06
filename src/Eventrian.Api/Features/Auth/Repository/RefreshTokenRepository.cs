using Eventrian.Api.Data;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Eventrian.Api.Features.Auth.Repository;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RefreshToken?> FindByTokenAsync(string token)
    {
        return await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task<string?> GetUserIdForTokenAsync(string token, DateTime now)
    {
        var tokenEntity = await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token && t.ExpiresAt > now);

        return tokenEntity?.UserId;
    }

    public async Task<List<RefreshToken>> GetExpiredOrUsedTokensAsync(DateTime now, TimeSpan overlapWindow)
    {
        return await _db.RefreshTokens
            .Where(t => t.ExpiresAt <= now || (t.UsedAt != null && t.UsedAt <= now - overlapWindow))
            .ToListAsync();
    }

    public async Task<List<string>> GetAllUserIdsWithTokensAsync()
    {
        return await _db.RefreshTokens
            .Where(t => t.UserId != null)
            .Select(t => t.UserId!)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<RefreshToken>> GetExcessTokensAsync(string userId, int maxTokens)
    {
        return await _db.RefreshTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip(maxTokens)
            .ToListAsync();
    }

    public void Add(RefreshToken token)
    {
        _db.RefreshTokens.Add(token);
    }

    public void Remove(RefreshToken token)
    {
        _db.RefreshTokens.Remove(token);
    }

    public void RemoveRange(IEnumerable<RefreshToken> tokens)
    {
        _db.RefreshTokens.RemoveRange(tokens);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
