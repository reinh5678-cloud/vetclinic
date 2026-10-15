using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class HospitalizationService : IHospitalizationService
{
    private const string VetRoleName = "Veterinario";
    private const int MaxListSize = 200;

    private static readonly HospitalizationStatus[] ActiveStatuses =
    {
        HospitalizationStatus.Ingresado,
        HospitalizationStatus.EnTratamiento,
        HospitalizationStatus.EnRecuperacion
    };

    private static readonly Dictionary<HospitalizationStatus, HospitalizationStatus[]> AllowedTransitions = new()
    {
        [HospitalizationStatus.Ingresado] = new[]
        {
            HospitalizationStatus.EnTratamiento,
            HospitalizationStatus.EnRecuperacion,
            HospitalizationStatus.Alta,
            HospitalizationStatus.Fallecido
        },
        [HospitalizationStatus.EnTratamiento] = new[]
        {
            HospitalizationStatus.EnRecuperacion,
            HospitalizationStatus.Alta,
            HospitalizationStatus.Fallecido
        },
        [HospitalizationStatus.EnRecuperacion] = new[]
        {
            HospitalizationStatus.EnTratamiento,
            HospitalizationStatus.Alta,
            HospitalizationStatus.Fallecido
        },
        [HospitalizationStatus.Alta] = Array.Empty<HospitalizationStatus>(),
        [HospitalizationStatus.Fallecido] = Array.Empty<HospitalizationStatus>()
    };

    private readonly IRepository<Hospitalization> _hospitalizations;
    private readonly IRepository<HospitalizationEvolution> _evolutions;
    private readonly IRepository<MedicationAdministration> _medications;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<User> _users;
    private readonly ICurrentUser _currentUser;

    public HospitalizationService(
        IRepository<Hospitalization> hospitalizations,
        IRepository<HospitalizationEvolution> evolutions,
        IRepository<MedicationAdministration> medications,
        IRepository<Pet> pets,
        IRepository<User> users,
        ICurrentUser currentUser)
    {
        _hospitalizations = hospitalizations;
        _evolutions = evolutions;
        _medications = medications;
        _pets = pets;
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<HospitalizationDto> StartAsync(StartHospitalizationRequest r, CancellationToken ct = default)
    {
        await EnsurePetAsync(r.PetId, ct);
        await EnsureVetAsync(r.AssignedVetId, ct);

        var alreadyActive = await _hospitalizations.Query()
            .AnyAsync(h => h.PetId == r.PetId && ActiveStatuses.Contains(h.Status), ct);
        if (alreadyActive)
            throw new InvalidOperationException("El paciente ya tiene una hospitalización activa");

        var h = new Hospitalization
        {
            PetId = r.PetId,
            AssignedVetId = r.AssignedVetId,
            Reason = Required(r.Reason, "Motivo", 300),
            Notes = Optional(r.Notes, "Notas", 2000),
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
            .AsNoTracking()
            .Where(h => ActiveStatuses.Contains(h.Status))
            .OrderBy(h => h.StartDate)
            .Include(h => h.Pet)
            .Include(h => h.AssignedVet)
            .Take(MaxListSize)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<HospitalizationDto>> ListAsync(HospitalizationStatus? status, int? petId, CancellationToken ct = default)
    {
        var query = _hospitalizations.Query().AsNoTracking();

        if (status.HasValue)
            query = query.Where(h => h.Status == status.Value);
        if (petId.HasValue)
            query = query.Where(h => h.PetId == petId.Value);

        var list = await query
            .OrderByDescending(h => h.StartDate)
            .Include(h => h.Pet)
            .Include(h => h.AssignedVet)
            .Take(MaxListSize)
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
        EnsureActive(h, "registrar una evolución");

        ValidateVitals(r.Temperature, r.HeartRate, r.RespiratoryRate);

        await _evolutions.AddAsync(new HospitalizationEvolution
        {
            HospitalizationId = id,
            Temperature = r.Temperature,
            HeartRate = r.HeartRate,
            RespiratoryRate = r.RespiratoryRate,
            Observations = Optional(r.Observations, "Observaciones", 2000),
            RecordedById = _currentUser.UserId
        }, ct);
        await _evolutions.SaveChangesAsync(ct);
        return await ReloadAsync(id, ct);
    }

    public async Task<HospitalizationDetailDto?> AddMedicationAsync(int id, AddMedicationAdministrationRequest r, CancellationToken ct = default)
    {
        var h = await _hospitalizations.GetByIdAsync(id, ct);
        if (h is null) return null;
        EnsureActive(h, "administrar medicación");

        await _medications.AddAsync(new MedicationAdministration
        {
            HospitalizationId = id,
            MedicationName = Required(r.MedicationName, "Medicamento", 120),
            Dose = Optional(r.Dose, "Dosis", 60),
            Route = Optional(r.Route, "Vía", 40),
            AdministeredAt = r.AdministeredAt == default ? DateTime.UtcNow : r.AdministeredAt,
            AdministeredById = _currentUser.UserId
        }, ct);
        await _medications.SaveChangesAsync(ct);
        return await ReloadAsync(id, ct);
    }

    public async Task<HospitalizationDetailDto?> ChangeStatusAsync(int id, HospitalizationStatus status, CancellationToken ct = default)
    {
        var h = await _hospitalizations.GetByIdAsync(id, ct);
        if (h is null) return null;

        if (!AllowedTransitions.GetValueOrDefault(h.Status, Array.Empty<HospitalizationStatus>()).Contains(status))
            throw new InvalidOperationException($"No se puede pasar de «{h.Status}» a «{status}»");

        ApplyStatus(h, status);
        _hospitalizations.Update(h);
        await _hospitalizations.SaveChangesAsync(ct);
        return await ReloadAsync(id, ct);
    }

    public async Task<HospitalizationDetailDto?> DischargeAsync(int id, CancellationToken ct = default)
    {
        var h = await _hospitalizations.GetByIdAsync(id, ct);
        if (h is null) return null;

        if (h.Status == HospitalizationStatus.Alta)
            throw new InvalidOperationException("La hospitalización ya está dada de alta");
        if (h.Status == HospitalizationStatus.Fallecido)
            throw new InvalidOperationException("No se puede dar de alta a un paciente fallecido");

        ApplyStatus(h, HospitalizationStatus.Alta);
        _hospitalizations.Update(h);
        await _hospitalizations.SaveChangesAsync(ct);
        return await ReloadAsync(id, ct);
    }

    private static void ApplyStatus(Hospitalization h, HospitalizationStatus status)
    {
        h.Status = status;
        if (status == HospitalizationStatus.Alta || status == HospitalizationStatus.Fallecido)
            h.EndDate ??= DateTime.UtcNow;
        else
            h.EndDate = null;
    }

    private static void EnsureActive(Hospitalization h, string action)
    {
        if (h.Status == HospitalizationStatus.Alta || h.Status == HospitalizationStatus.Fallecido)
            throw new InvalidOperationException($"No se puede {action} en una hospitalización finalizada");
    }

    private async Task<HospitalizationDetailDto> ReloadAsync(int id, CancellationToken ct)
    {
        var loaded = await LoadDetailAsync(id, ct);
        if (loaded is null) throw new KeyNotFoundException("La hospitalización indicada ya no existe");
        return MapDetail(loaded);
    }

    private async Task<Hospitalization?> LoadDetailAsync(int id, CancellationToken ct) =>
        await _hospitalizations.Query()
            .AsSplitQuery()
            .Include(h => h.Pet)
            .Include(h => h.AssignedVet)
            .Include(h => h.Evolutions).ThenInclude(e => e.RecordedBy)
            .Include(h => h.MedicationAdministrations).ThenInclude(m => m.AdministeredBy)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

    private async Task EnsurePetAsync(int petId, CancellationToken ct)
    {
        if (!await _pets.Query().AnyAsync(p => p.Id == petId, ct))
            throw new ArgumentException("El paciente indicado no existe");
    }

    private async Task EnsureVetAsync(int? vetId, CancellationToken ct)
    {
        if (vetId is null) return;
        var vet = await _users.Query().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == vetId, ct);
        if (vet is null)
            throw new ArgumentException("El veterinario indicado no existe");
        if (!vet.IsActive || vet.Role?.Name != VetRoleName)
            throw new ArgumentException("El usuario indicado no es un veterinario activo");
    }

    private static void ValidateVitals(decimal? temperature, int? heartRate, int? respiratoryRate)
    {
        if (temperature is < 0 or > 60m)
            throw new ArgumentException("La temperatura debe estar entre 0 y 60 °C");
        if (heartRate is < 0 or > 400)
            throw new ArgumentException("La frecuencia cardíaca debe estar entre 0 y 400 lpm");
        if (respiratoryRate is < 0 or > 200)
            throw new ArgumentException("La frecuencia respiratoria debe estar entre 0 y 200 rpm");
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException($"El campo «{field}» es obligatorio");
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"El campo «{field}» supera el máximo de {maxLength} caracteres");
        return trimmed;
    }

    private static string Optional(string? value, string field, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"El campo «{field}» supera el máximo de {maxLength} caracteres");
        return trimmed;
    }

    private static HospitalizationDto Map(Hospitalization h)
    {
        ArgumentNullException.ThrowIfNull(h);
        return new(
            h.Id, h.StartDate, h.EndDate, h.Status.ToString(), h.Reason, h.PetId, h.Pet?.Name, h.AssignedVetId, h.AssignedVet?.FullName);
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
            h.AssignedVetId,
            h.AssignedVet?.FullName,
            h.Evolutions
                .OrderByDescending(e => e.RecordedAt)
                .Select(e => new EvolutionDto(e.Id, e.RecordedAt, e.Temperature, e.HeartRate, e.RespiratoryRate, e.Observations, e.RecordedBy?.FullName))
                .ToList(),
            h.MedicationAdministrations
                .OrderByDescending(m => m.AdministeredAt)
                .Select(m => new MedicationDto(m.Id, m.MedicationName, m.Dose, m.Route, m.AdministeredAt, m.AdministeredBy?.FullName))
                .ToList());
    }
}
