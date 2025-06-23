using System.Net.Http.Json;
using Eventrian.Client.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Client.Features.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly ITokenStorageService _tokenStorage;

    public AuthService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _http = http;
        _tokenStorage = tokenStorage;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                return errorResult ?? LoginResponseDto.FailureResponse("Login failed.");
            }
            catch
            {
                // Fallback if API returns HTML/text/plain error or malformed response
                return LoginResponseDto.FailureResponse($"Login failed with status {response.StatusCode}.");
            }
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return result ?? LoginResponseDto.FailureResponse("Empty response from server.");
    }


    public async Task LogoutAsync()
    {
        await _tokenStorage.RemoveTokenAsync();
    }
}
