using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Provides authentication-related operations such as login, registration, logout, and token refresh.
/// </summary>
public interface IAuthService
{
    Task InitializeAsync();

    /// <summary>
    /// Sends a login request and stores the access and refresh tokens if successful.
    /// </summary>
    /// <param name="request">The login request containing user credentials and remember-me preference.</param>
    /// <returns>
    /// A <see cref="LoginResponseDto"/> containing the authentication result, access token, and refresh token if successful.
    /// </returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Sends a registration request and stores the access and refresh tokens if successful.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <returns>
    /// A <see cref="LoginResponseDto"/> containing the registration result, access token, and refresh token if successful.
    /// </returns>
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Clears stored access and refresh tokens and notifies the app of logout.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Retrieves the currently stored access token, if any.
    /// </summary>
    /// <returns>The access token string or null if not authenticated.</returns>
    string? GetAccessToken();

    /// <summary>
    /// Attempts to refresh the access token using a stored refresh token.
    /// </summary>
    /// <returns>True if the token was refreshed successfully; otherwise, false.</returns>
    Task<bool> TryRefreshTokenAsync();
}
