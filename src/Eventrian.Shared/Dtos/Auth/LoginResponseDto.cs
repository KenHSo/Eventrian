namespace Eventrian.Shared.Dtos.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
}
