using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Authorization;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;
using VetClinic.Domain.Enums;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly IVaccineService _vaccines;

    public PatientsController(IPatientService service, IVaccineService vaccines)
    {
        _service = service;
        _vaccines = vaccines;
    }

    [HttpPost("tutors")]
    [RequirePermission(Permissions.PatientsManage)]
    public async Task<IActionResult> CreateTutor(CreateTutorRequest request, CancellationToken ct) =>
        Ok(await _service.CreateTutorAsync(request, ct));

    [HttpGet("tutors")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> ListTutors(CancellationToken ct) =>
        Ok(await _service.ListTutorsAsync(ct));

    [HttpGet("tutors/list")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> ListTutorsWithPets(CancellationToken ct) =>
        Ok(await _service.ListTutorsWithPetsAsync(ct));

    [HttpGet("tutors/{id:int}")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> GetTutor(int id, CancellationToken ct)
    {
        var tutor = await _service.GetTutorAsync(id, ct);
        return tutor is null ? NotFound() : Ok(tutor);
    }

    [HttpPut("tutors/{id:int}")]
    [RequirePermission(Permissions.PatientsManage)]
    public async Task<IActionResult> UpdateTutor(int id, UpdateTutorRequest request, CancellationToken ct)
    {
        var tutor = await _service.UpdateTutorAsync(id, request, ct);
        return tutor is null ? NotFound() : Ok(tutor);
    }

    [HttpPost("pets")]
    [RequirePermission(Permissions.PatientsManage)]
    public async Task<IActionResult> CreatePet(CreatePetRequest request, CancellationToken ct) =>
        Ok(await _service.CreatePetAsync(request, ct));

    [HttpGet("pets")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> ListPets([FromQuery] string? search, [FromQuery] string? species, [FromQuery] bool? active, CancellationToken ct)
    {
        Species? speciesFilter = null;
        if (!string.IsNullOrWhiteSpace(species) && Enum.TryParse<Species>(species, true, out var parsed))
            speciesFilter = parsed;
        return Ok(await _service.ListPetsAsync(search, speciesFilter, active, ct));
    }

    [HttpGet("pets/{id:int}")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> GetPet(int id, CancellationToken ct)
    {
        var pet = await _service.GetPetAsync(id, ct);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPut("pets/{id:int}")]
    [RequirePermission(Permissions.PatientsManage)]
    public async Task<IActionResult> UpdatePet(int id, UpdatePetRequest request, CancellationToken ct)
    {
        var pet = await _service.UpdatePetAsync(id, request, ct);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpPut("pets/{id:int}/active")]
    [RequirePermission(Permissions.PatientsManage)]
    public async Task<IActionResult> SetPetActive(int id, SetPetActiveRequest request, CancellationToken ct)
    {
        var pet = await _service.SetPetActiveAsync(id, request.Active, ct);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpGet("tutors/{tutorId:int}/pets")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> ListPetsByTutor(int tutorId, CancellationToken ct) =>
        Ok(await _service.ListPetsByTutorAsync(tutorId, ct));

    [HttpPost("pets/{petId:int}/vaccines")]
    [RequirePermission(Permissions.PatientsManage)]
    public async Task<IActionResult> AddVaccine(int petId, AddVaccineRequest request, CancellationToken ct)
    {
        var vaccine = await _vaccines.AddAsync(petId, request, ct);
        return vaccine is null ? NotFound() : Ok(vaccine);
    }

    [HttpGet("pets/{petId:int}/vaccines")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> ListVaccines(int petId, CancellationToken ct) =>
        Ok(await _vaccines.ListByPetAsync(petId, ct));

    [HttpGet("vaccines/upcoming")]
    [RequirePermission(Permissions.PatientsView)]
    public async Task<IActionResult> ListUpcomingVaccines([FromQuery] int days = 30, CancellationToken ct = default) =>
        Ok(await _vaccines.ListUpcomingAsync(days, ct));
}
