using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class ReportService : IReportService
{
    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<Appointment> _appointments;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Tutor> _tutors;
    private readonly IRepository<InventoryItem> _inventory;
    private readonly IRepository<Hospitalization> _hospitalizations;
    private readonly IRepository<VaccineApplication> _vaccines;
    private readonly IRepository<Reminder> _reminders;

    public ReportService(
        IRepository<Invoice> invoices,
        IRepository<Appointment> appointments,
        IRepository<Pet> pets,
        IRepository<Tutor> tutors,
        IRepository<InventoryItem> inventory,
        IRepository<Hospitalization> hospitalizations,
        IRepository<VaccineApplication> vaccines,
        IRepository<Reminder> reminders)
    {
        _invoices = invoices;
        _appointments = appointments;
        _pets = pets;
        _tutors = tutors;
        _inventory = inventory;
        _hospitalizations = hospitalizations;
        _vaccines = vaccines;
        _reminders = reminders;
    }

    public async Task<DashboardDto> GetDashboardAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var current = await _invoices.Query()
            .Where(i => i.Status == InvoiceStatus.Pagada && i.IssueDate >= from && i.IssueDate <= to)
            .Select(i => i.Total)
            .ToListAsync(ct);

        var span = to - from;
        var previousFrom = from - span;
        var previous = await _invoices.Query()
            .Where(i => i.Status == InvoiceStatus.Pagada && i.IssueDate >= previousFrom && i.IssueDate < from)
            .Select(i => i.Total)
            .ToListAsync(ct);

        var revenue = current.Sum();
        var previousRevenue = previous.Sum();

        return new DashboardDto(
            new PeriodDto(from, to),
            revenue,
            previousRevenue,
            previousRevenue == 0m
                ? null
                : Math.Round((revenue - previousRevenue) / previousRevenue * 100m, 1),
            current.Count,
            await _pets.Query().CountAsync(ct),
            await _tutors.Query().CountAsync(ct),
            await _appointments.Query().CountAsync(
                a => a.Status == AppointmentStatus.Programada && a.StartTime >= from && a.StartTime <= to, ct),
            await _inventory.Query().CountAsync(i => i.IsActive && i.StockQuantity <= i.MinStock, ct));
    }

    public async Task<RevenueReportDto> GetRevenuesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var paid = await _invoices.Query()
            .Where(i => i.Status == InvoiceStatus.Pagada && i.IssueDate >= from && i.IssueDate <= to)
            .Select(i => new { i.Total, i.IssueDate })
            .ToListAsync(ct);

        var byMonth = paid
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new RevenuePointDto(g.Key.Year, g.Key.Month, g.Sum(x => x.Total)))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        return new RevenueReportDto(paid.Sum(i => i.Total), byMonth);
    }

    public async Task<RetentionDto> GetClientRetentionAsync(CancellationToken ct = default)
    {
        var tutors = await _tutors.Query()
            .Include(t => t.Pets).ThenInclude(p => p.Appointments)
            .Include(t => t.Invoices)
            .ToListAsync(ct);

        var active = tutors.Count;
        var returning = tutors.Count(t =>
            t.Invoices.Count > 1 || t.Pets.Any(p => p.Appointments.Count > 1));

        return new RetentionDto(
            active,
            returning,
            active == 0 ? decimal.Zero : Math.Round((decimal)returning / active * 100, 1));
    }

    public async Task<OperationsDto> GetOperationsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var upcomingLimit = today.AddDays(30);

        var appointmentsToday = await _appointments.Query()
            .CountAsync(a => a.StartTime >= today && a.StartTime < tomorrow, ct);

        var appointmentsPending = await _appointments.Query()
            .CountAsync(a => a.Status == AppointmentStatus.Programada && a.StartTime >= today, ct);

        var appointmentsInProgress = await _appointments.Query()
            .CountAsync(a => a.Status == AppointmentStatus.EnEspera
                || a.Status == AppointmentStatus.EnConsulta, ct);

        var urgentOpen = await _appointments.Query()
            .CountAsync(a => a.Urgency >= UrgencyLevel.Alta
                && a.Status != AppointmentStatus.Completada
                && a.Status != AppointmentStatus.Cancelada
                && a.Status != AppointmentStatus.NoAsistio, ct);

        var activeHospitalizations = await _hospitalizations.Query()
            .CountAsync(h => h.Status != HospitalizationStatus.Alta
                && h.Status != HospitalizationStatus.Fallecido, ct);

        var upcomingVaccines = await _vaccines.Query()
            .CountAsync(v => v.NextDueDate != null
                && v.NextDueDate >= today
                && v.NextDueDate <= upcomingLimit, ct);

        var pendingReminders = await _reminders.Query().CountAsync(r => !r.IsSent, ct);

        var unpaid = await _invoices.Query()
            .Where(i => i.Status == InvoiceStatus.Emitida)
            .Select(i => i.Total)
            .ToListAsync(ct);

        return new OperationsDto(
            appointmentsToday,
            appointmentsPending,
            appointmentsInProgress,
            urgentOpen,
            activeHospitalizations,
            upcomingVaccines,
            pendingReminders,
            unpaid.Count,
            unpaid.Sum());
    }

    public async Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(CancellationToken ct = default) =>
        await _inventory.Query()
            .Where(i => i.IsActive && i.StockQuantity <= i.MinStock)
            .OrderBy(i => i.StockQuantity)
            .Select(i => new LowStockItemDto(
                i.Id, i.Name, i.Category, i.Code, i.Unit, i.StockQuantity, i.MinStock, i.Supplier))
            .ToListAsync(ct);
}
