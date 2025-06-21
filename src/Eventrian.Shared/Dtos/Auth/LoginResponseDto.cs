namespace Eventrian.Shared.Dtos.Auth;

public class LoginResponseDto
{
    public string? Email { get; set; }
    public string? Token { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
}
