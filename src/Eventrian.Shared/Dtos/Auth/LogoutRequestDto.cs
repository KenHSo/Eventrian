using System.ComponentModel.DataAnnotations;

namespace Eventrian.Shared.Dtos.Auth;

/// <summary>
/// Contains the refresh token required to log out and invalidate the session.
/// </summary>
public class LogoutRequestDto
{
    [Required]
    public required string RefreshToken { get; set; }
}
