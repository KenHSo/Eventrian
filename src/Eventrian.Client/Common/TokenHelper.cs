using Eventrian.Shared.Dtos.Auth.Interfaces;
using System.IdentityModel.Tokens.Jwt;

public static class TokenHelper
{
    /// <summary>
    /// Determines whether the given response is valid by ensuring it is not null,
    /// indicates success, and contains both an access token and a refresh token.
    /// </summary>
    /// <typeparam name="T">The DTO type implementing <see cref="IAuthResponse"/>.</typeparam>
    /// <param name="result">The response object to validate.</param>
    /// <returns><c>true</c> if the response is valid and contains both tokens; otherwise, <c>false</c>.</returns>
    public static bool IsValidTokenResponse<T>(T? result)
        where T : class, IAuthResponse
    {
        return result is not null &&
               result.Success &&
               !string.IsNullOrWhiteSpace(result.AccessToken) &&
               !string.IsNullOrWhiteSpace(result.RefreshToken);
    }

    /// <summary>
    /// Determines whether a JWT access token is expired, optionally using an early expiration threshold.
    /// </summary>
    /// <param name="token">The raw JWT access token string.</param>
    /// <param name="earlyThreshold">
    /// Optional buffer duration to consider the token expired before its actual expiration time.
    /// Useful for proactive refresh scenarios.
    /// </param>
    /// <returns><c>true</c> if the token is expired or invalid; otherwise, <c>false</c>.</returns>
    public static bool IsExpired(string token, TimeSpan? earlyThreshold = null)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var threshold = earlyThreshold ?? TimeSpan.Zero;
            return jwt.ValidTo < DateTime.UtcNow + threshold;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TokenHelper] Failed to parse token: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Attempts to parse a JWT string into a <see cref="JwtSecurityToken"/>.
    /// </summary>
    /// <param name="token">The raw JWT token string.</param>
    /// <returns>The parsed <see cref="JwtSecurityToken"/> if successful; otherwise, <c>null</c>.</returns>
    public static JwtSecurityToken? TryParseJwt(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(token);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the user ID ("sub" claim) from a JWT access token.
    /// </summary>
    public static string GetUserIdFromAccessToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Subject ?? ""; // Safe fallback: token creation always include 'sub'; fallback avoids null propagation if tampered
    }
}
