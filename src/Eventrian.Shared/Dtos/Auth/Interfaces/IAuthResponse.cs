namespace Eventrian.Shared.Dtos.Auth.Interfaces;

/// <summary>
/// Represents a standard authentication response with access and refresh tokens.
/// </summary>
public interface IAuthResponse
{
    bool Success { get; }
    string? AccessToken { get; }
    string? RefreshToken { get; }
}
