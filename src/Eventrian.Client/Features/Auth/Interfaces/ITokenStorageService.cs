namespace Eventrian.Client.Features.Auth.Interfaces;

public interface ITokenStorageService
{
    Task SetTokenAsync(string token);
    Task<string?> GetTokenAsync();
    Task RemoveTokenAsync();
}
