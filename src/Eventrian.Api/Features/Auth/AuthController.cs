using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Eventrian.Api.Models;
using Eventrian.Shared.Dtos.Auth;

namespace Eventrian.Api.Features.Auth;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // TODO: Replace "mock-register-token" with actual JWT generated from ITokenService
        return Ok(new AuthResponseDto
        {
            Token = "mock-register-token",
            Message = "User registered successfully"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid) 
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(loginDto.Email);

        if (user == null) 
            return Unauthorized("Invalid email or password");

        var result = await _signInManager.PasswordSignInAsync(
            user, 
            loginDto.Password, 
            isPersistent: loginDto.RememberMe, 
            lockoutOnFailure: true
            );

        if (!result.Succeeded)
            return Unauthorized("Invalid email or password");

        // TODO: Replace "mock-login-token" with actual JWT generated from ITokenService
        return Ok(new AuthResponseDto {
                Token = "mock-login-token",
                Message = "Login successful" 
            });
    }

}
