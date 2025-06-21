namespace Eventrian.Shared.Dtos.Auth;

public class LoginResponseDto
{
    public bool Success { get; set; } = false;
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
}
