using System.ComponentModel.DataAnnotations;

namespace Eventrian.Shared.Dtos.Auth;

/// <summary>
/// Contains the refresh token required to obtain a new access token.
/// </summary>
public class RefreshRequestDto
{
    [Required]
    public required string RefreshToken { get; set; }
}
