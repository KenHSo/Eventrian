using Microsoft.JSInterop;
using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class RefreshTokenStorageService : IRefreshTokenStorageService
{
    private readonly IJSRuntime _js;

    private const string RefreshTokenKey = "refresh_token";

    public RefreshTokenStorageService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetRefreshTokenAsync(string token, bool rememberMe)
    {
        if (rememberMe)
        {
            // Store in localStorage only
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, token);
        }
        else
        {
            // Store in sessionStorage only
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshTokenKey, token);
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string>("sessionStorage.getItem", RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(token)) return token;

            token = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
            return token;
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"JS interop failed: {jsEx.Message}");
            return null;
        }
    }

    public async Task RemoveRefreshTokenAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
    }

    public async Task<bool> HasLocalStorageTokenAsync()
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }
}
