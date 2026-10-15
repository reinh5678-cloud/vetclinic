using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/settings/users")]
[Authorize(Roles = "Administrador")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _users.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
        => Ok(await _users.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _users.UpdateAsync(id, request, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
        => await _users.ResetPasswordAsync(id, request, ct) ? NoContent() : NotFound();

    [HttpPost("{id:int}/pin")]
    public async Task<IActionResult> SetPin(int id, SetPinRequest request, CancellationToken ct)
    {
        var user = await _users.SetPinAsync(id, request, ct);
        return user is null ? NotFound() : Ok(user);
    }
}
