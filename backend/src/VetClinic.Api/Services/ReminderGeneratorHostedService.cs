using Microsoft.EntityFrameworkCore;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Infrastructure.Data;

namespace VetClinic.Api.Services;

/// <summary>
/// Genera recordatorios para las vacunas próximas a vencer según la configuración
/// de notificaciones. Los recordatorios se persisten para su envío (log-only mientras
/// no haya un proveedor SMTP activo).
/// </summary>
public class ReminderGeneratorHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private const int DefaultReminderDaysBefore = 7;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderGeneratorHostedService> _logger;

    public ReminderGeneratorHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderGeneratorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar los recordatorios de vacunas.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task GenerateAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();

        var notifications = await settings.GetNotificationsAsync(ct);
        var daysBefore = notifications.VaccineReminderDaysBefore > 0
            ? notifications.VaccineReminderDaysBefore
            : DefaultReminderDaysBefore;

        var now = DateTime.UtcNow;
        var horizon = now.AddDays(daysBefore);

        var due = await db.VaccineApplications
            .Where(v => v.NextDueDate != null
                        && v.NextDueDate >= now
                        && v.NextDueDate <= horizon
                        && v.Pet != null
                        && v.Pet.IsActive)
            .Include(v => v.Pet)
                .ThenInclude(p => p!.Tutor)
            .Include(v => v.InventoryItem)
            .ToListAsync(ct);

        if (due.Count == 0)
        {
            _logger.LogDebug("Sin vacunas próximas a vencer en los siguientes {Days} días.", daysBefore);
            return;
        }

        var petIds = due.Select(v => v.PetId).Distinct().ToList();
        var existing = await db.Reminders
            .Where(r => r.PetId != null && petIds.Contains(r.PetId.Value))
            .Select(r => new { r.PetId, r.ScheduledAt })
            .ToListAsync(ct);

        var taken = existing
            .Select(e => (PetId: e.PetId!.Value, Date: e.ScheduledAt.Date))
            .ToHashSet();

        var channel = notifications.Channels.FirstOrDefault() ?? "Email";
        var useEmail = channel.Equals("Email", StringComparison.OrdinalIgnoreCase);
        var created = 0;

        foreach (var vaccine in due)
        {
            if (vaccine.Pet is null || vaccine.NextDueDate is null)
                continue;

            var nextDue = vaccine.NextDueDate.Value;
            var scheduledAt = nextDue.AddDays(-daysBefore);

            if (!taken.Add((vaccine.PetId, scheduledAt.Date)))
                continue;

            var tutor = vaccine.Pet.Tutor;
            var recipient = useEmail
                ? tutor?.Email ?? string.Empty
                : tutor?.Phone ?? string.Empty;

            var vaccineName = vaccine.InventoryItem?.Name ?? "vacuna";

            db.Reminders.Add(new Reminder
            {
                PetId = vaccine.PetId,
                Recipient = recipient,
                Channel = channel,
                Message = $"Recordatorio: la vacuna «{vaccineName}» de {vaccine.Pet.Name} vence el {nextDue:yyyy-MM-dd}.",
                ScheduledAt = scheduledAt,
                IsSent = false,
            });
            created++;
        }

        if (created == 0)
            return;

        await db.SaveChangesAsync(ct);

        if (notifications.Enabled && !string.IsNullOrWhiteSpace(notifications.SmtpHost))
            _logger.LogInformation(
                "Generados {Count} recordatorios de vacunas. SMTP configurado ({Host}:{Port}); listo para envío.",
                created, notifications.SmtpHost, notifications.SmtpPort);
        else
            _logger.LogInformation(
                "Generados {Count} recordatorios de vacunas (modo log-only, SMTP sin configurar).",
                created);
    }
}
