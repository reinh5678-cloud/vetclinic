using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _auth.LoginAsync(request, ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("login-pin")]
    [Authorize]
    public async Task<IActionResult> LoginPin(LoginPinRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _auth.SwitchByPinAsync(request.Pin, ct);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> Permissions(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        var permissions = await _auth.GetPermissionsAsync(userId, ct);
        return Ok(new PermissionsResponse(permissions.ToArray()));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new { user = User.Identity!.Name });

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var users = await _auth.ListUsersAsync(ct);
        return Ok(users);
    }
}