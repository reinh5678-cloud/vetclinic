using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class HospitalizationService : IHospitalizationService
{
    private readonly IRepository<Hospitalization> _hospitalizations;
    private readonly IRepository<HospitalizationEvolution> _evolutions;
    private readonly IRepository<MedicationAdministration> _medications;

    public HospitalizationService(
        IRepository<Hospitalization> hospitalizations,
        IRepository<HospitalizationEvolution> evolutions,
        IRepository<MedicationAdministration> medications)
    {
        _hospitalizations = hospitalizations;
        _evolutions = evolutions;
        _medications = medications;
    }

    public async Task<HospitalizationDto> StartAsync(StartHospitalizationRequest r, CancellationToken ct = default)
    {
        var h = new Hospitalization
        {
            PetId = r.PetId,
            AssignedVetId = r.AssignedVetId,
            Reason = r.Reason,
            Notes = r.Notes,
            Status = HospitalizationStatus.Ingresado
        };
        await _hospitalizations.AddAsync(h, ct);
        await _hospitalizations.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(h.Id, ct);
        if (loaded is null) throw new InvalidOperationException("Failed to load hospitalization after creation");
        return Map(loaded);
    }

    public async Task<IReadOnlyList<HospitalizationDto>> ListActiveAsync(CancellationToken ct = default)
    {
        var list = await _hospitalizations.Query()
            .Where(h => h.Status == HospitalizationStatus.Ingresado || h.Status == HospitalizationStatus.EnTratamiento || h.Status == HospitalizationStatus.EnRecuperacion)
            .OrderBy(h => h.StartDate)
            .Include(h => h.Pet)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<HospitalizationDetailDto?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var h = await LoadDetailAsync(id, ct);
        return h is null ? null : MapDetail(h);
    }

    public async Task<HospitalizationDetailDto?> AddEvolutionAsync(int id, AddEvolutionRequest r, CancellationToken ct = default)
    {
        var h = await _hospitalizations.GetByIdAsync(id, ct);
        if (h is null) return null;
        await _evolutions.AddAsync(new HospitalizationEvolution
        {
            HospitalizationId = id,
            Temperature = r.Temperature,
            HeartRate = r.HeartRate,
            RespiratoryRate = r.RespiratoryRate,
            Observations = r.Observations
        }, ct);
        await _evolutions.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(id, ct);
        return loaded is null ? null : MapDetail(loaded);
    }

    public async Task<HospitalizationDetailDto?> AddMedicationAsync(int id, AddMedicationAdministrationRequest r, CancellationToken ct = default)
    {
        var h = await _hospitalizations.GetByIdAsync(id, ct);
        if (h is null) return null;
        await _medications.AddAsync(new MedicationAdministration
        {
            HospitalizationId = id,
            MedicationName = r.MedicationName,
            Dose = r.Dose,
            Route = r.Route,
            AdministeredAt = r.AdministeredAt == default ? DateTime.UtcNow : r.AdministeredAt
        }, ct);
        await _medications.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(id, ct);
        return loaded is null ? null : MapDetail(loaded);
    }

    public async Task<HospitalizationDetailDto?> DischargeAsync(int id, CancellationToken ct = default)
    {
        var h = await _hospitalizations.GetByIdAsync(id, ct);
        if (h is null) return null;
        h.Status = HospitalizationStatus.Alta;
        h.EndDate = DateTime.UtcNow;
        _hospitalizations.Update(h);
        await _hospitalizations.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(id, ct);
        return loaded is null ? null : MapDetail(loaded);
    }

    private async Task<Hospitalization?> LoadDetailAsync(int id, CancellationToken ct) =>
        await _hospitalizations.Query()
            .Include(h => h.Pet)
            .Include(h => h.Evolutions)
            .Include(h => h.MedicationAdministrations)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

    private static HospitalizationDto Map(Hospitalization h)
    {
        ArgumentNullException.ThrowIfNull(h);
        return new(
            h.Id, h.StartDate, h.EndDate, h.Status.ToString(), h.Reason, h.PetId, h.Pet?.Name, h.AssignedVetId);
    }

    private static HospitalizationDetailDto MapDetail(Hospitalization h)
    {
        ArgumentNullException.ThrowIfNull(h);
        ArgumentNullException.ThrowIfNull(h.Evolutions);
        ArgumentNullException.ThrowIfNull(h.MedicationAdministrations);
        return new(
            h.Id,
            h.StartDate,
            h.EndDate,
            h.Status.ToString(),
            h.Reason,
            h.Notes,
            h.PetId,
            h.Pet?.Name,
            h.Evolutions
                .OrderByDescending(e => e.RecordedAt)
                .Select(e => new EvolutionDto(e.Id, e.RecordedAt, e.Temperature, e.HeartRate, e.RespiratoryRate, e.Observations))
                .ToList(),
            h.MedicationAdministrations
                .OrderByDescending(m => m.AdministeredAt)
                .Select(m => new MedicationDto(m.Id, m.MedicationName, m.Dose, m.Route, m.AdministeredAt))
                .ToList());
    }
}