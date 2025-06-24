namespace Eventrian.Api.Features.Auth.Interfaces;

/// <summary>
/// Service for generating JWT access tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed JWT access token for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="roles">A list of roles assigned to the user.</param>
    /// <param name="username">The user's username.</param>
    /// <returns>A JWT access token as a string.</returns>
    string CreateToken(string userId, string email, string username, IList<string> roles);
}
