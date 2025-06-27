using Eventrian.Shared.Dtos.Auth.Interfaces;

namespace Eventrian.Shared.Dtos.Auth;

public class LoginResponseDto : IAuthResponse
{
    public bool Success { get; set; } = false;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful login response with access and refresh tokens.
    /// </summary>
    /// <param name="email">The authenticated user's email address.</param>
    /// <param name="accessToken">The issued JWT access token.</param>
    /// <param name="refreshToken">The issued refresh token.</param>
    /// <param name="message">An optional success message. Defaults to "Operation successful."</param>
    /// <returns>A <see cref="LoginResponseDto"/> representing a successful login.</returns>
    public static LoginResponseDto SuccessResponse(
        string email,
        string accessToken,
        string refreshToken,
        string message = "Operation successful.")
    {
        return new LoginResponseDto
        {
            Success = true,
            Email = email,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Message = message,
            Errors = null
        };
    }

    /// <summary>
    /// Creates a failed login response with an optional error list.
    /// </summary>
    /// <param name="message">A descriptive error message.</param>
    /// <param name="errors">Optional list of detailed error messages.</param>
    /// <returns>A <see cref="LoginResponseDto"/> indicating failure.</returns>
    public static LoginResponseDto FailureResponse(string message, List<string>? errors = null)
    {
        return new LoginResponseDto
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}
