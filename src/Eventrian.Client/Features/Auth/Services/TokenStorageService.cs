using Microsoft.JSInterop;
using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class TokenStorageService(IJSRuntime js) : ITokenStorageService
{
    private const string TokenKey = "authToken";

    public async Task SetTokenAsync(string token)
    {
        await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await js.InvokeAsync<string>("localStorage.getItem", TokenKey);
    }

    public async Task RemoveTokenAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }
}
