using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.Common;
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

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        return Ok(await _auth.GetProfileAsync(userId, ct));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        try
        {
            return Ok(await _auth.UpdateProfileAsync(userId, request, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        try
        {
            await _auth.ChangePasswordAsync(userId, request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("change-pin")]
    [Authorize]
    public async Task<IActionResult> ChangePin(ChangePinRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        try
        {
            return Ok(await _auth.ChangePinAsync(userId, request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out int userId)
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [HttpGet("security-questions/catalog")]
    [AllowAnonymous]
    public IActionResult QuestionCatalog() => Ok(SecurityQuestions.Catalog());

    [HttpGet("recovery/questions")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoveryQuestions([FromQuery] string username, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.GetRecoveryQuestionsAsync(username, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("recovery/password")]
    [AllowAnonymous]
    public async Task<IActionResult> RecoverPassword(RecoverPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _auth.RecoverPasswordAsync(request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("security-questions")]
    [Authorize]
    public async Task<IActionResult> MySecurityQuestions(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        try
        {
            return Ok(await _auth.GetMySecurityQuestionsAsync(userId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("security-questions")]
    [Authorize]
    public async Task<IActionResult> SetSecurityQuestions(SetSecurityQuestionsRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token sin identificador de usuario" });
        try
        {
            return Ok(await _auth.SetSecurityQuestionsAsync(userId, request, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var users = await _auth.ListUsersAsync(ct);
        return Ok(users);
    }
}