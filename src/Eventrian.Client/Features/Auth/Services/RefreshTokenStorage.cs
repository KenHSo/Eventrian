using Microsoft.JSInterop;
using Eventrian.Client.Features.Auth.Interfaces;

namespace Eventrian.Client.Features.Auth.Services;

public class RefreshTokenStorage : IRefreshTokenStorage
{
    private readonly IJSRuntime _js;

    private const string RefreshTokenKey = "refresh_token";
    private const string StorageTypeKey = "active_token_storage"; // "session" or "local"
    private const string RefreshInProgressKey = "refresh_in_progress";

    public RefreshTokenStorage(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetRefreshTokenAsync(string token, bool rememberMe)
    {
        if (rememberMe)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, token);
            await _js.InvokeVoidAsync("sessionStorage.setItem", StorageTypeKey, "local");
        }
        else
        {
            await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshTokenKey, token);
            await _js.InvokeVoidAsync("sessionStorage.setItem", StorageTypeKey, "session");
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var storageType = await _js.InvokeAsync<string>("sessionStorage.getItem", StorageTypeKey);

        if (storageType == "session")
        {
            var sessionToken = await _js.InvokeAsync<string>("sessionStorage.getItem", RefreshTokenKey);
            return string.IsNullOrWhiteSpace(sessionToken) ? null : sessionToken;
        }

        if (storageType == "local")
        {
            var localToken = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
            return string.IsNullOrWhiteSpace(localToken) ? null : localToken;
        }

        // Fallback: storageType missing (new tab?), but token might be in localStorage
        var fallbackToken = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
        if (!string.IsNullOrWhiteSpace(fallbackToken))
        {
            Console.WriteLine("[RefreshTokenStorage] Fallback: found token in localStorage with unknown storageType.");
            return fallbackToken;
        }

        Console.WriteLine("[RefreshTokenStorage] No valid storage type or fallback token found");
        return null;
    }

    public async Task RemoveRefreshTokenAsync()
    {
        var storageType = await _js.InvokeAsync<string>("sessionStorage.getItem", StorageTypeKey);

        if (storageType == "session")
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);
        }
        else if (storageType == "local")
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        }

        await _js.InvokeVoidAsync("sessionStorage.removeItem", StorageTypeKey);
    }

    public async Task<bool> HasLocalStorageTokenAsync()
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<bool> IsRefreshInProgressAsync()
    {
        var value = await _js.InvokeAsync<string>("sessionStorage.getItem", RefreshInProgressKey);
        return value == "true";
    }

    public async Task SetRefreshInProgressAsync(bool isInProgress)
    {
        if (isInProgress)
            await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshInProgressKey, "true");
        else
            await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshInProgressKey);
    }
}
