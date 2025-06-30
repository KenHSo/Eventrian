namespace Eventrian.Api.Features.Auth.Models;

/// <summary>
/// Represents the result of validating and rotating a refresh token.
/// </summary>
public sealed class TokenValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the refresh token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the new refresh token issued after rotation.
    /// This value is <c>null</c> if the validation failed.
    /// </summary>
    public string? NewRefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the refresh token.
    /// This value is <c>null</c> if the validation failed.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Indicates whether the refresh token was persistent (e.g., intended for long-term use in localStorage).
    /// </summary>
    public bool? IsPersistent { get; set; }
}
