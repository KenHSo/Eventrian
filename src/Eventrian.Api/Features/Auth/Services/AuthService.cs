using Eventrian.Api.Features.Auth.Models;
using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Shared.Constants;
using Eventrian.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Identity;

namespace Eventrian.Api.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AuthService> _logger;


    public AuthService(UserManager<ApplicationUser> userManager, IAccessTokenService accessTokenService, IRefreshTokenService refreshTokenService, ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);   
        
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", loginDto.Email);
            return LoginResponseDto.FailureResponse("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var accessToken = _accessTokenService.CreateAccessToken(user.Id, user.Email!, user.UserName!, roles);
        var refreshToken = await _refreshTokenService.IssueRefreshTokenAsync(user.Id, loginDto.RememberMe);

        _logger.LogInformation("User {Email} logged in successfully.", user.Email);
        return LoginResponseDto.SuccessResponse(user.Email!, accessToken, refreshToken, "Login successful.");
    }
   
    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        
        if (existingUser != null)
        {
            _logger.LogWarning("Attempted registration with existing email: {Email}", registerDto.Email);
            return LoginResponseDto.FailureResponse("User already exists.");
        }

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

        await _userManager.AddToRoleAsync(newUser, AppRoles.Customer);

        var roles = await _userManager.GetRolesAsync(newUser);

        var accessToken = _accessTokenService.CreateAccessToken(newUser.Id, newUser.Email!, newUser.UserName, roles);
        var refreshToken = await _refreshTokenService.IssueRefreshTokenAsync(newUser.Id, false);

        _logger.LogInformation("User {Email} registered successfully.", newUser.Email);

        return LoginResponseDto.SuccessResponse(newUser.Email!, accessToken, refreshToken, "User registered successfully.");
    }
 
    public async Task<RefreshResponseDto> RefreshTokenAsync(RefreshRequestDto request)
    {
        // Validate the refresh token and rotate it (issue a new one to replace the old)
        var result = await _refreshTokenService.ValidateAndRotateAsync(request.RefreshToken);
        
        if (!result.IsValid || result.UserId == null || result.NewRefreshToken == null)
        {
            _logger.LogWarning("Refresh token validation failed for token: {Token}", request.RefreshToken);
            return RefreshResponseDto.FailureResponse("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(result.UserId);
        
        if (user == null)
        {
            _logger.LogWarning("Refresh token passed validation but user {UserId} was not found.", result.UserId);
            return RefreshResponseDto.FailureResponse("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _accessTokenService.CreateAccessToken(user.Id, user.Email!, user.UserName!, roles);

        _logger.LogInformation("Refresh token successful for user {UserId}.", user.Id);

        return RefreshResponseDto.SuccessResponse(newAccessToken, result.NewRefreshToken, "Token refreshed.");
    }

    public async Task<LogoutResponseDto> RevokeRefreshTokenAsync(LogoutRequestDto logoutRequest)
    {
        var userId = await _refreshTokenService.GetUserIdForToken(logoutRequest.RefreshToken);
        
        if (userId == null)
            return LogoutResponseDto.FailureResponse("Invalid refresh token.");

        await _refreshTokenService.RevokeRefreshTokensAsync(logoutRequest.RefreshToken);

        _logger.LogInformation("User {UserId} logged out and refresh token revoked.", userId);

        return LogoutResponseDto.SuccessResponse("Logged out and refresh token invalidated.");
    }
}
