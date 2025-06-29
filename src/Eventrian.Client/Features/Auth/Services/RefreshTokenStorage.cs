using Microsoft.JSInterop;
using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class RefreshTokenStorage : IRefreshTokenStorage
{
    private readonly IJSRuntime _js;

    private const string RefreshTokenKey = "refresh_token";

    public RefreshTokenStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetRefreshTokenAsync(string token, bool rememberMe)
    {
        if (rememberMe)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, token);
        }
        else
        {
            await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshTokenKey, token);
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            var sessionToken = await _js.InvokeAsync<string>("sessionStorage.getItem", RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                Console.WriteLine("[RefreshTokenStorage] Using sessionStorage token");
                return sessionToken;
            }

            var localToken = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(localToken))
            {
                Console.WriteLine("[RefreshTokenStorage] Using localStorage token");
                return localToken;
            }

            Console.WriteLine("[RefreshTokenStorage] No token found");
            return null;
        }
        catch (JSException jsEx)
        {
            Console.WriteLine($"[RefreshTokenStorage] JS interop failed: {jsEx.Message}");
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
