using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Models;
using Eventrian.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Identity;

namespace Eventrian.Api.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(UserManager<ApplicationUser> userManager, IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService)
    {
        _userManager = userManager;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);   
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            return LoginResponseDto.FailureResponse("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _accessTokenService.CreateAccessToken(user.Id, user.Email!, user.UserName!, roles);
        var refreshToken = await _refreshTokenService.IssueRefreshTokenAsync(user.Id);

        return LoginResponseDto.SuccessResponse(user.Email!, accessToken, refreshToken, "Login successful.");
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
            return LoginResponseDto.FailureResponse("User already exists.");

        var newUser = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
        };

        var result = await _userManager.CreateAsync(newUser, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return LoginResponseDto.FailureResponse("Registration failed.", errors);
        }

        await _userManager.AddToRoleAsync(newUser, "Customer");

        var roles = await _userManager.GetRolesAsync(newUser);

        var accessToken = _accessTokenService.CreateAccessToken(newUser.Id, newUser.Email!, newUser.UserName, roles);
        var refreshToken = await _refreshTokenService.IssueRefreshTokenAsync(newUser.Id);

        return LoginResponseDto.SuccessResponse(newUser.Email!, accessToken, refreshToken, "User registered successfully.");
    }

    public async Task<RefreshResponseDto> RefreshTokenAsync(RefreshRequestDto request)
    {
        // Validate the refresh token and rotate it (issue a new one to replace the old)
        var result = await _refreshTokenService.ValidateAndRotateAsync(request.RefreshToken);
        if (!result.IsValid || result.UserId == null || result.NewRefreshToken == null)
            return RefreshResponseDto.FailureResponse("Invalid or expired refresh token.");

        var user = await _userManager.FindByIdAsync(result.UserId);
        if (user == null)
            return RefreshResponseDto.FailureResponse("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _accessTokenService.CreateAccessToken(user.Id, user.Email!, user.UserName!, roles);

        return RefreshResponseDto.SuccessResponse(newAccessToken, result.NewRefreshToken, "Token refreshed.");
    }

    public async Task<LogoutResponseDto> RevokeRefreshTokenAsync(LogoutRequestDto logoutRequest)
    {
        var userId = await _refreshTokenService.GetUserIdForToken(logoutRequest.RefreshToken);
        if (userId == null)
            return LogoutResponseDto.FailureResponse("Invalid refresh token.");

        await _refreshTokenService.RevokeRefreshTokensAsync(logoutRequest.RefreshToken);

        return LogoutResponseDto.SuccessResponse("Logged out and refresh token invalidated.");
    }
}
