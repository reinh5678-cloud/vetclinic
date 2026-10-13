using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/hospitalizations")]
[Authorize(Roles = "Veterinario,Administrador")]
public class HospitalizationsController : ControllerBase
{
    private readonly IHospitalizationService _service;

    public HospitalizationsController(IHospitalizationService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Start(StartHospitalizationRequest request, CancellationToken ct) =>
        Ok(await _service.StartAsync(request, ct));

    [HttpGet("active")]
    public async Task<IActionResult> ListActive(CancellationToken ct) =>
        Ok(await _service.ListActiveAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var h = await _service.GetDetailAsync(id, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPost("{id:int}/evolutions")]
    public async Task<IActionResult> AddEvolution(int id, AddEvolutionRequest request, CancellationToken ct)
    {
        var h = await _service.AddEvolutionAsync(id, request, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPost("{id:int}/medications")]
    public async Task<IActionResult> AddMedication(int id, AddMedicationAdministrationRequest request, CancellationToken ct)
    {
        var h = await _service.AddMedicationAsync(id, request, ct);
        return h is null ? NotFound() : Ok(h);
    }

    [HttpPost("{id:int}/discharge")]
    public async Task<IActionResult> Discharge(int id, CancellationToken ct)
    {
        var h = await _service.DischargeAsync(id, ct);
        return h is null ? NotFound() : Ok(h);
    }
}