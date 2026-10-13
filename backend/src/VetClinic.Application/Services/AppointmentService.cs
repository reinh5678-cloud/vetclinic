using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class AppointmentService : IAppointmentService
{
    private static readonly Dictionary<Domain.Enums.AppointmentStatus, Domain.Enums.AppointmentStatus[]> AllowedTransitions = new()
    {
        [Domain.Enums.AppointmentStatus.Programada] = new[]
        {
            Domain.Enums.AppointmentStatus.EnEspera,
            Domain.Enums.AppointmentStatus.EnConsulta,
            Domain.Enums.AppointmentStatus.Completada,
            Domain.Enums.AppointmentStatus.Cancelada,
            Domain.Enums.AppointmentStatus.NoAsistio
        },
        [Domain.Enums.AppointmentStatus.EnEspera] = new[]
        {
            Domain.Enums.AppointmentStatus.EnConsulta,
            Domain.Enums.AppointmentStatus.Completada,
            Domain.Enums.AppointmentStatus.Cancelada,
            Domain.Enums.AppointmentStatus.NoAsistio
        },
        [Domain.Enums.AppointmentStatus.EnConsulta] = new[]
        {
            Domain.Enums.AppointmentStatus.Completada,
            Domain.Enums.AppointmentStatus.Cancelada
        },
        [Domain.Enums.AppointmentStatus.Completada] = Array.Empty<Domain.Enums.AppointmentStatus>(),
        [Domain.Enums.AppointmentStatus.Cancelada] = Array.Empty<Domain.Enums.AppointmentStatus>(),
        [Domain.Enums.AppointmentStatus.NoAsistio] = new[]
        {
            Domain.Enums.AppointmentStatus.Programada
        }
    };

    private readonly IRepository<Appointment> _appointments;

    public AppointmentService(IRepository<Appointment> appointments)
    {
        _appointments = appointments;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest r, CancellationToken ct = default)
    {
        if (r.StartTime < DateTime.UtcNow.AddMinutes(-5))
            throw new ArgumentException("La fecha de la cita no puede estar en el pasado");

        var overlap = await _appointments.Query()
            .AnyAsync(a => a.VetId == r.VetId && a.Status == Domain.Enums.AppointmentStatus.Programada &&
                a.StartTime < r.StartTime.AddMinutes(r.DurationMinutes) &&
                a.StartTime.AddMinutes(a.DurationMinutes) > r.StartTime, ct);

        if (overlap)
            throw new InvalidOperationException("El veterinario ya tiene una cita en ese horario");

        var appt = new Appointment
        {
            StartTime = r.StartTime,
            DurationMinutes = r.DurationMinutes,
            Urgency = r.Urgency,
            Reason = r.Reason,
            PetId = r.PetId,
            VetId = r.VetId
        };
        await _appointments.AddAsync(appt, ct);
        await _appointments.SaveChangesAsync(ct);
        return Map(appt);
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
            !AllowedTransitions.GetValueOrDefault(appt.Status, Array.Empty<Domain.Enums.AppointmentStatus>()).Contains(r.Status))
            throw new InvalidOperationException($"No se puede pasar la cita de «{appt.Status}» a «{r.Status}»");

        appt.Status = r.Status;
        if (!string.IsNullOrWhiteSpace(r.Notes)) appt.Notes = r.Notes;
        _appointments.Update(appt);
        await _appointments.SaveChangesAsync(ct);
        return Map((await _appointments.Query().Include(a => a.Pet).Include(a => a.Vet).FirstOrDefaultAsync(a => a.Id == id, ct))!);
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var appt = await _appointments.Query().Include(a => a.Pet).Include(a => a.Vet).FirstOrDefaultAsync(a => a.Id == id, ct);
        return appt is null ? null : Map(appt);
    }

    private static AppointmentDto Map(Appointment a) => new(
        a.Id, a.StartTime, a.DurationMinutes, a.Urgency.ToString(), a.Status.ToString(), a.Reason, a.Notes,
        a.PetId, a.Pet?.Name, a.VetId, a.Vet?.FullName);
}