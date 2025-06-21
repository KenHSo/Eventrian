using Microsoft.AspNetCore.Mvc;
using Eventrian.Shared.Dtos.Auth;
using Eventrian.Api.Features.Auth.Interfaces;

namespace Eventrian.Api.Features.Auth;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

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
}
