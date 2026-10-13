using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _service;

    public AppointmentsController(IAppointmentService service) => _service = service;

    [HttpPost]
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

    [HttpGet("upcoming")]
    public async Task<IActionResult> ListUpcoming(CancellationToken ct) =>
        Ok(await _service.ListUpcomingAsync(ct));

    [HttpGet("pet/{petId:int}")]
    public async Task<IActionResult> ListByPet(int petId, CancellationToken ct) =>
        Ok(await _service.ListByPetAsync(petId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var appointment = await _service.GetByIdAsync(id, ct);
        return appointment is null ? NotFound() : Ok(appointment);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateAppointmentStatusRequest request, CancellationToken ct)
    {
        var appointment = await _service.UpdateStatusAsync(id, request, ct);
        return appointment is null ? NotFound() : Ok(appointment);
    }
}