using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Administrador")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles)
    {
        _roles = roles;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await _roles.ListAsync(ct));

    [HttpGet("roles/{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var role = await _roles.GetAsync(id, ct);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken ct)
        => Ok(await _roles.CreateAsync(request, ct));


    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _roles.UpdateAsync(id, request, ct);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpGet("permissions")]
    public IActionResult ListPermissions()
        => Ok(Permissions.Catalog.Select(p => new PermissionDto(p.Code, p.Group, p.Label)));
}
