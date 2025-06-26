namespace Eventrian.Api.Features.Auth.Interfaces;

public interface IRefreshTokenService
{
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task<string> RotateRefreshTokenAsync(string userId);
    Task<string?> GetUserIdForToken(string refreshToken);

}

