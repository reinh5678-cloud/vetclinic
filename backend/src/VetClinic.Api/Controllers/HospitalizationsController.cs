using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Authorization;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;
using VetClinic.Domain.Enums;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/hospitalizations")]
[Authorize]
public class HospitalizationsController : ControllerBase
{
    private readonly IHospitalizationService _service;

    public HospitalizationsController(IHospitalizationService service) => _service = service;

    [HttpPost]
    [RequirePermission(Permissions.HospitalizationManage)]
    public async Task<IActionResult> Start(StartHospitalizationRequest request, CancellationToken ct) =>
        Ok(await _service.StartAsync(request, ct));

    [HttpGet("active")]
    [RequirePermission(Permissions.HospitalizationView)]
    public async Task<IActionResult> ListActive(CancellationToken ct) =>
        Ok(await _service.ListActiveAsync(ct));

    [HttpGet]
    [RequirePermission(Permissions.HospitalizationView)]
    public async Task<IActionResult> List(string? status, int? petId, CancellationToken ct)
    {
        HospitalizationStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<HospitalizationStatus>(status, ignoreCase: true, out var value))
                return BadRequest($"Estado de hospitalización inválido: «{status}»");
            parsed = value;
        }

        return Ok(await _service.ListAsync(parsed, petId, ct));
    }

    [HttpGet("{id:int}")]
    [RequirePermission(Permissions.HospitalizationView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var h = await _service.GetDetailAsync(id, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPost("{id:int}/evolutions")]
    [RequirePermission(Permissions.HospitalizationManage)]
    public async Task<IActionResult> AddEvolution(int id, AddEvolutionRequest request, CancellationToken ct)
    {
        var h = await _service.AddEvolutionAsync(id, request, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPost("{id:int}/medications")]
    [RequirePermission(Permissions.HospitalizationManage)]
    public async Task<IActionResult> AddMedication(int id, AddMedicationAdministrationRequest request, CancellationToken ct)
    {
        var h = await _service.AddMedicationAsync(id, request, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPatch("{id:int}/status")]
    [RequirePermission(Permissions.HospitalizationManage)]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<HospitalizationStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest($"Estado de hospitalización inválido: «{request.Status}»");

        var h = await _service.ChangeStatusAsync(id, status, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPost("{id:int}/discharge")]
    [RequirePermission(Permissions.HospitalizationManage)]
    public async Task<IActionResult> Discharge(int id, CancellationToken ct)
    {
        var h = await _service.DischargeAsync(id, ct);
        return h is null ? NotFound() : Ok(h);
    }
}
