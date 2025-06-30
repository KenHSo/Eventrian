using Eventrian.Api.Features.Auth.Interfaces;
using Eventrian.Shared.Dtos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventrian.Api.Features.Auth;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto registerRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RegisterAsync(registerRequest);
        if (!response.Success)
            return BadRequest(new { errors = response.Errors ?? new List<string> { response.Message } });

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto loginRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.LoginAsync(loginRequest);
        if (!response.Success)
            return Unauthorized(response.Message);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshRequestDto refreshRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RefreshTokenAsync(refreshRequest);
        if (!response.Success)
            return Unauthorized(response.Message);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequestDto logoutRequest)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _authService.RevokeRefreshTokenAsync(logoutRequest);
        if (!response.Success)
            return BadRequest(response.Message);

        return Ok(response);
    }

    // TODO: Remove test endpoint before production
    [HttpGet("protected")]
    public IActionResult ProtectedEndpoint()
    {
        return Ok("TEST OK - You have accessed a protected endpoint.");
    }
}
