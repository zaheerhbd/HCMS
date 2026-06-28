using HCM.Application.Auth.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthDataHandler _authHandler;

    public AuthController(IAuthDataHandler authHandler) => _authHandler = authHandler;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authHandler.LoginAsync(request.Username, request.Password, ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        // TODO: Implement RefreshTokenService
        return Unauthorized();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        // TODO: Implement LogoutService
        return NoContent();
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string Token);
