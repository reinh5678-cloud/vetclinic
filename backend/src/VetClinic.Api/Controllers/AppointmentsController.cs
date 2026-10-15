using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Authorization;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;
using VetClinic.Domain.Enums;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service) => _service = service;

    [HttpPost]
    [RequirePermission(Permissions.AppointmentsManage)]
    public async Task<IActionResult> Create(CreateAppointmentRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CreateAsync(request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission(Permissions.AppointmentsManage)]
    public async Task<IActionResult> Update(int id, UpdateAppointmentRequest request, CancellationToken ct)
    {
        try
        {
            var appointment = await _service.UpdateAsync(id, request, ct);
            return appointment is null ? NotFound() : Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [RequirePermission(Permissions.AppointmentsView)]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? vetId,
        [FromQuery] string? status,
        [FromQuery] int? petId,
        CancellationToken ct)
    {
        AppointmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var parsed))
            statusFilter = parsed;
        return Ok(await _service.ListAsync(from, to, vetId, statusFilter, petId, ct));
    }

    [HttpGet("upcoming")]
    [RequirePermission(Permissions.AppointmentsView)]
    public async Task<IActionResult> ListUpcoming(CancellationToken ct) =>
        Ok(await _service.ListUpcomingAsync(ct));

    [HttpGet("pet/{petId:int}")]
    [RequirePermission(Permissions.AppointmentsView)]
    public async Task<IActionResult> ListByPet(int petId, CancellationToken ct) =>
        Ok(await _service.ListByPetAsync(petId, ct));

    [HttpGet("{id:int}")]
    [RequirePermission(Permissions.AppointmentsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var appointment = await _service.GetByIdAsync(id, ct);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpPatch("{id:int}/status")]
    [RequirePermission(Permissions.AppointmentsManage)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateAppointmentStatusRequest request, CancellationToken ct)
    {
        try
        {
            var appointment = await _service.UpdateStatusAsync(id, request, ct);
            return appointment is null ? NotFound() : Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
