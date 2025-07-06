namespace Eventrian.Api.Features.Auth.Results;

/// <summary>
/// Represents the result of validating and rotating a refresh token.
/// </summary>
public sealed class RefreshTokenValidationResult
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

    /// <summary>
    /// Creates a successful token validation result.
    /// </summary>
    /// <param name="userId">The user ID associated with the token.</param>
    /// <param name="newToken">The new refresh token issued.</param>
    /// <param name="isPersistent">Whether the token is persistent.</param>
    /// <returns>A populated <see cref="RefreshTokenValidationResult"/> indicating success.</returns>
    public static RefreshTokenValidationResult Success(string userId, string newToken, bool isPersistent) => new()
    {
        IsValid = true,
        NewRefreshToken = newToken,
        UserId = userId,
        IsPersistent = isPersistent
    };

    /// <summary>
    /// Creates a failed token validation result.
    /// </summary>
    /// <returns>A <see cref="RefreshTokenValidationResult"/> indicating failure.</returns>
    public static RefreshTokenValidationResult Failure() => new()
    {
        IsValid = false,
        NewRefreshToken = null,
        UserId = null,
        IsPersistent = null
    };


}
