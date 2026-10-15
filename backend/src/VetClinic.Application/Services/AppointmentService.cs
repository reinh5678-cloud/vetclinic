using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class AppointmentService : IAppointmentService
{
    private const string VetRoleName = "Veterinario";

    private static readonly AppointmentStatus[] ActiveStatuses =
    {
        AppointmentStatus.Programada,
        AppointmentStatus.EnEspera,
        AppointmentStatus.EnConsulta
    };

    private static readonly Dictionary<AppointmentStatus, AppointmentStatus[]> AllowedTransitions = new()
    {
        [AppointmentStatus.Programada] = new[]
        {
            AppointmentStatus.EnEspera,
            AppointmentStatus.EnConsulta,
            AppointmentStatus.Completada,
            AppointmentStatus.Cancelada,
            AppointmentStatus.NoAsistio
        },
        [AppointmentStatus.EnEspera] = new[]
        {
            AppointmentStatus.EnConsulta,
            AppointmentStatus.Completada,
            AppointmentStatus.Cancelada,
            AppointmentStatus.NoAsistio
        },
        [AppointmentStatus.EnConsulta] = new[]
        {
            AppointmentStatus.Completada,
            AppointmentStatus.Cancelada
        },
        [AppointmentStatus.Completada] = Array.Empty<AppointmentStatus>(),
        [AppointmentStatus.Cancelada] = Array.Empty<AppointmentStatus>(),
        [AppointmentStatus.NoAsistio] = new[]
        {
            AppointmentStatus.Programada
        }
    };

    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<User> _users;
    private readonly ISettingsService _settings;

    public AppointmentService(
        IRepository<Appointment> appointments,
        IRepository<Pet> pets,
        IRepository<User> users,
        ISettingsService settings)
    {
        _appointments = appointments;
        _pets = pets;
        _users = users;
        _settings = settings;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest r, CancellationToken ct = default)
    {
        await ValidateScheduleAsync(r.StartTime, r.DurationMinutes, ct);
        await EnsurePetAsync(r.PetId, ct);
        await EnsureVetAsync(r.VetId, ct);
        await EnsureNoOverlapAsync(r.VetId, r.StartTime, r.DurationMinutes, null, ct);

        var appt = new Appointment
        {
            StartTime = r.StartTime,
            DurationMinutes = r.DurationMinutes,
            Urgency = r.Urgency,
            Reason = (r.Reason ?? string.Empty).Trim(),
            PetId = r.PetId,
            VetId = r.VetId,
            Status = AppointmentStatus.Programada
        };
        await _appointments.AddAsync(appt, ct);
        await _appointments.SaveChangesAsync(ct);
        return await GetDtoAsync(appt.Id, ct) ?? Map(appt);
    }

    public async Task<AppointmentDto?> UpdateAsync(int id, UpdateAppointmentRequest r, CancellationToken ct = default)
    {
        var appt = await _appointments.GetByIdAsync(id, ct);
        if (appt is null) return null;
        if (IsTerminal(appt.Status))
            throw new InvalidOperationException($"No se puede reprogramar una cita «{appt.Status}»");

        await ValidateScheduleAsync(r.StartTime, r.DurationMinutes, ct);
        await EnsurePetAsync(r.PetId, ct);
        await EnsureVetAsync(r.VetId, ct);
        await EnsureNoOverlapAsync(r.VetId, r.StartTime, r.DurationMinutes, id, ct);

        appt.StartTime = r.StartTime;
        appt.DurationMinutes = r.DurationMinutes;
        appt.Urgency = r.Urgency;
        appt.Reason = (r.Reason ?? string.Empty).Trim();
        appt.PetId = r.PetId;
        appt.VetId = r.VetId;
        _appointments.Update(appt);
        await _appointments.SaveChangesAsync(ct);
        return await GetDtoAsync(id, ct);
    }

    public async Task<IReadOnlyList<AppointmentDto>> ListAsync(DateTime? from, DateTime? to, int? vetId, AppointmentStatus? status, int? petId, CancellationToken ct = default)
    {
        var query = _appointments.Query();
        if (from.HasValue) query = query.Where(a => a.StartTime >= from.Value);
        if (to.HasValue) query = query.Where(a => a.StartTime < to.Value);
        if (vetId.HasValue) query = query.Where(a => a.VetId == vetId.Value);
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        if (petId.HasValue) query = query.Where(a => a.PetId == petId.Value);

        var list = await query
            .OrderBy(a => a.StartTime)
            .Include(a => a.Pet)
            .Include(a => a.Vet)
            .Take(500)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AppointmentDto>> ListUpcomingAsync(CancellationToken ct = default)
    {
        var list = await _appointments.Query()
            .Where(a => a.StartTime.Date >= DateTime.UtcNow.Date.AddDays(-1))
            .OrderBy(a => a.StartTime)
            .Include(a => a.Pet)
            .Include(a => a.Vet)
            .Take(200)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AppointmentDto>> ListByPetAsync(int petId, CancellationToken ct = default)
    {
        var list = await _appointments.Query()
            .Where(a => a.PetId == petId)
            .OrderByDescending(a => a.StartTime)
            .Include(a => a.Pet)
            .Include(a => a.Vet)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<AppointmentDto?> UpdateStatusAsync(int id, UpdateAppointmentStatusRequest r, CancellationToken ct = default)
    {
        var appt = await _appointments.GetByIdAsync(id, ct);
        if (appt is null) return null;

        if (appt.Status != r.Status &&
            !AllowedTransitions.GetValueOrDefault(appt.Status, Array.Empty<AppointmentStatus>()).Contains(r.Status))
            throw new InvalidOperationException($"No se puede pasar la cita de «{appt.Status}» a «{r.Status}»");

        if (appt.Status != r.Status && ActiveStatuses.Contains(r.Status))
            await EnsureNoOverlapAsync(appt.VetId, appt.StartTime, appt.DurationMinutes, id, ct);

        appt.Status = r.Status;
        if (!string.IsNullOrWhiteSpace(r.Notes)) appt.Notes = r.Notes.Trim();
        _appointments.Update(appt);
        await _appointments.SaveChangesAsync(ct);
        return await GetDtoAsync(id, ct);
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default) => await GetDtoAsync(id, ct);

    private async Task<AppointmentDto?> GetDtoAsync(int id, CancellationToken ct)
    {
        var appt = await _appointments.Query()
            .Include(a => a.Pet)
            .Include(a => a.Vet)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        return appt is null ? null : Map(appt);
    }

    private async Task ValidateScheduleAsync(DateTime start, int durationMinutes, CancellationToken ct)
    {
        var agenda = await _settings.GetAgendaAsync(ct);
        if (durationMinutes < agenda.SlotMinutes || durationMinutes > 480)
            throw new ArgumentException($"La duración debe estar entre {agenda.SlotMinutes} y 480 minutos");

        var utcStart = start.Kind == DateTimeKind.Local
            ? start.ToUniversalTime()
            : DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (utcStart < DateTime.UtcNow.AddMinutes(-5))
            throw new ArgumentException("La fecha de la cita no puede estar en el pasado");

        var tz = _settings.GetAgendaTimeZone();
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(utcStart, tz);
        var localEnd = localStart.AddMinutes(durationMinutes);

        if (TimeSpan.TryParse(agenda.OpeningTime, CultureInfo.InvariantCulture, out var opening) &&
            TimeSpan.TryParse(agenda.ClosingTime, CultureInfo.InvariantCulture, out var closing) &&
            closing > opening)
        {
            if (localEnd.Date != localStart.Date || localStart.TimeOfDay < opening || localEnd.TimeOfDay > closing)
                throw new ArgumentException($"La cita está fuera del horario de atención ({agenda.OpeningTime}\u2013{agenda.ClosingTime})");
        }

        if (agenda.ClosedWeekdays is { Length: > 0 } &&
            agenda.ClosedWeekdays.Any(d => Enum.TryParse<DayOfWeek>(d, true, out var day) && day == localStart.DayOfWeek))
            throw new ArgumentException($"La clínica no atiende los {WeekdayName(localStart.DayOfWeek)}");

        if (agenda.MaxAdvanceDays > 0)
        {
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            if (localStart.Date > todayLocal.AddDays(agenda.MaxAdvanceDays))
                throw new ArgumentException($"Solo se puede agendar hasta {agenda.MaxAdvanceDays} días de antelación");
        }
    }

    private static string WeekdayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábados",
        _ => "domingos"
    };

    private async Task EnsurePetAsync(int petId, CancellationToken ct)
    {
        if (!await _pets.Query().AnyAsync(p => p.Id == petId, ct))
            throw new ArgumentException("El paciente indicado no existe");
    }

    private async Task EnsureVetAsync(int vetId, CancellationToken ct)
    {
        var vet = await _users.Query().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == vetId, ct);
        if (vet is null)
            throw new ArgumentException("El veterinario indicado no existe");
        if (!vet.IsActive || vet.Role?.Name != VetRoleName)
            throw new ArgumentException("El usuario indicado no es un veterinario activo");
    }

    private async Task EnsureNoOverlapAsync(int? vetId, DateTime start, int durationMinutes, int? excludeId, CancellationToken ct)
    {
        if (vetId is null) return;
        var end = start.AddMinutes(durationMinutes);
        var query = _appointments.Query().Where(a =>
            a.VetId == vetId &&
            ActiveStatuses.Contains(a.Status) &&
            a.StartTime < end &&
            a.StartTime.AddMinutes(a.DurationMinutes) > start);
        if (excludeId.HasValue)
            query = query.Where(a => a.Id != excludeId.Value);
        if (await query.AnyAsync(ct))
            throw new InvalidOperationException("El veterinario ya tiene una cita en ese horario");
    }

    private static bool IsTerminal(AppointmentStatus status) =>
        status is AppointmentStatus.Completada or AppointmentStatus.Cancelada;

    private static AppointmentDto Map(Appointment a) => new(
        a.Id, a.StartTime, a.DurationMinutes, a.Urgency.ToString(), a.Status.ToString(), a.Reason, a.Notes,
        a.PetId, a.Pet?.Name, a.VetId, a.Vet?.FullName);
}
