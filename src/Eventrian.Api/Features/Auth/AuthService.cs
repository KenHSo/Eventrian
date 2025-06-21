using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Api.Models;
using Eventrian.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Identity;

namespace Eventrian.Api.Features.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            return LoginResponseDto.FailureResponse("Invalid email or password.");
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        string token = _tokenService.CreateToken(user.Id, user.Email!, roles);

        return LoginResponseDto.SuccessResponse(user.Email!, token, "Login successful.");
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
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
            return LoginResponseDto.FailureResponse(
                "Registration failed.", result.Errors.Select(e => e.Description).ToList());
        }

        await _userManager.AddToRoleAsync(newUser, "Customer");

        IList<string> roles = await _userManager.GetRolesAsync(newUser);
        string token = _tokenService.CreateToken(newUser.Id, newUser.Email!, roles);

        return LoginResponseDto.SuccessResponse(newUser.Email!, token, "User registered successfully.");
    }
}
