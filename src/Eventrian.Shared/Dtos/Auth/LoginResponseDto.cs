namespace Eventrian.Shared.Dtos.Auth;

public class LoginResponseDto
{
    public bool Success { get; set; } = false;
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }

    public static LoginResponseDto SuccessResponse(string email, string token, string message = "Operation successful.")
    {
        return new LoginResponseDto
        {
            Success = true,
            Email = email,
            Token = token,
            Message = message,
            Errors = null
        };
    }

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
