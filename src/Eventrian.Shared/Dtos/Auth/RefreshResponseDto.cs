using Eventrian.Shared.Dtos.Auth.Interfaces;

namespace Eventrian.Shared.Dtos.Auth;

public class RefreshResponseDto : IAuthResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful refresh response with access and refresh tokens.
    /// </summary>
    /// <param name="accessToken">The newly issued JWT access token.</param>
    /// <param name="refreshToken">The newly issued refresh token.</param>
    /// <param name="message">An optional success message. Defaults to "Token refreshed successfully."</param>
    /// <returns>A <see cref="RefreshResponseDto"/> representing a successful token refresh.</returns>
    public static RefreshResponseDto SuccessResponse(string accessToken, string refreshToken, string message = "Token refreshed successfully.")
    {
        return new RefreshResponseDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failed refresh response.
    /// </summary>
    /// <param name="message">A descriptive error message.</param>
    /// <returns>A <see cref="RefreshResponseDto"/> indicating failure.</returns>
    public static RefreshResponseDto FailureResponse(string message)
    {
        return new RefreshResponseDto
        {
            Success = false,
            Message = message,
            AccessToken = null,
            RefreshToken = null
        };
    }
}

