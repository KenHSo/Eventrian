using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Client.Features.Auth.Interfaces;

/// <summary>
/// Provides authentication-related operations such as login, registration, and logout.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Sends a login request and stores the token if successful.
    /// </summary>
    /// <param name="request">The login request containing user credentials and options.</param>
    /// <returns>The login result with token if successful; otherwise, an error message.</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Sends a registration request and stores the token if successful.
    /// </summary>
    /// <param name="request">The registration request containing new user details.</param>
    /// <returns>The registration result with token if successful; otherwise, an error message.</returns>
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Removes the stored token and notifies the app of logout.
    /// </summary>
    Task LogoutAsync();
}
