using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Client.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task LogoutAsync();
}
