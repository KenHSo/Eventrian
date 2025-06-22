using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Api.Features.Auth.Interfaces;

/// <summary>
/// Service for authentication-related operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    /// <param name="loginRequest">The login request data containing user credentials.</param>
    /// <returns>A <see cref="LoginResponseDto"/> containing authentication token and messages.</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto loginRequest);

    /// <summary>
    /// Registers a new user with the provided information.
    /// </summary>
    /// <param name="registerRequest">The registration request data containing user details and password.</param>
    /// <returns>A <see cref="LoginResponseDto"/> containing authentication token and messages.</returns>
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerRequest);
}
