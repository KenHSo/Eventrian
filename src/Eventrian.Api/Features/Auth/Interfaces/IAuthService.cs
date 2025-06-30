using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Api.Features.Auth.Interfaces;

/// <summary>
/// Defines authentication-related operations such as login, registration, token refresh, and logout.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to authenticate a user using the provided credentials.
    /// </summary>
    /// <param name="loginRequest">The <see cref="LoginRequestDto"/> containing user credentials.</param>
    /// <returns>
    /// A <see cref="LoginResponseDto"/> containing tokens and a message if successful; 
    /// otherwise, an error response.
    /// </returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest);

    /// <summary>
    /// Registers a new user and assigns them a default role.
    /// </summary>
    /// <param name="registerRequest">The <see cref="RegisterRequestDto"/> containing new user details.</param>
    /// <returns>
    /// A <see cref="LoginResponseDto"/> containing tokens and success message;
    /// otherwise, an error response with validation messages.
    /// </returns>
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerRequest);

    /// <summary>
    /// Refreshes the access token using the provided refresh token.
    /// If the refresh token is valid, issues both a new access token and a new refresh token.
    /// </summary>
    /// <param name="refreshRequest">
    /// The <see cref="RefreshRequestDto"/> containing the refresh token to validate.
    /// </param>
    /// <returns>
    /// A <see cref="RefreshResponseDto"/> with new tokens if the refresh token is valid;
    /// otherwise, an error message.
    /// </returns>
    Task<RefreshResponseDto> RefreshTokenAsync(RefreshRequestDto refreshRequest);

    /// <summary>
    /// Revokes a refresh token, invalidating the session it represents.
    /// </summary>
    /// <param name="logoutRequest">The <see cref="LogoutRequestDto"/> with the refresh token to revoke.</param>
    /// <returns>
    /// A <see cref="LogoutResponseDto"/> indicating whether the operation was successful.
    /// </returns>
    Task<LogoutResponseDto> RevokeRefreshTokenAsync(LogoutRequestDto logoutRequest);
}
