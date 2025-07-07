using System.ComponentModel.DataAnnotations;

namespace Eventrian.Shared.Dtos.Auth;

/// <summary>
/// Represents the login credentials and preferences submitted by a user.
/// </summary>
public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// If true, the refresh token will be stored in Local Storage for a longer session.
    /// </summary>
    public bool RememberMe { get; set; } = false;
}
