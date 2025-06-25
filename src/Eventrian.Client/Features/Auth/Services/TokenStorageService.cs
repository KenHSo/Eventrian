using Microsoft.JSInterop;
using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class TokenStorageService(IJSRuntime js) : ITokenStorageService
{
    private const string AccessTokenKey = "accessToken";

    public async Task SetTokenAsync(string token, bool rememberMe)
    {
        await ClearAllStoragesAsync();

        if (rememberMe)
            await js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, token);
        else
            await js.InvokeVoidAsync("sessionStorage.setItem", AccessTokenKey, token);
    }

    public async Task<string?> GetTokenAsync()
    {
        var token = await js.InvokeAsync<string>("sessionStorage.getItem", AccessTokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            return token;

        return await js.InvokeAsync<string>("localStorage.getItem", AccessTokenKey);
    }

    public async Task RemoveTokenAsync()
    {
        await ClearAllStoragesAsync();
    }

    /// <summary>
    /// Removes the token from both sessionStorage and localStorage.
    /// </summary>
    private async Task ClearAllStoragesAsync()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("sessionStorage.removeItem", AccessTokenKey);
    }
}
