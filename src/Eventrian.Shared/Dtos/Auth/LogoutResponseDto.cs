namespace Eventrian.Shared.Dtos.Auth;

/// <summary>
/// Represents the result of a logout attempt, including status and messages.
/// </summary>
public class LogoutResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful logout response with an optional message.
    /// </summary>
    /// <param name="message">Optional success message. Defaults to "Logout successful."</param>
    /// <returns>A <see cref="LogoutResponseDto"/> indicating successful logout.</returns>
    public static LogoutResponseDto SuccessResponse(string message = "Logout successful.")
    {
        return new LogoutResponseDto
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failed logout response with a descriptive message.
    /// </summary>
    /// <param name="message">The reason for logout failure.</param>
    /// <returns>A <see cref="LogoutResponseDto"/> indicating failure.</returns>
    public static LogoutResponseDto FailureResponse(string message)
    {
        return new LogoutResponseDto
        {
            Success = false,
            Message = message
        };
    }
}
