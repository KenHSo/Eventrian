using System.ComponentModel.DataAnnotations;

namespace Eventrian.Shared.Dtos.Auth;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    public bool RememberMe { get; set; } = false;
}
