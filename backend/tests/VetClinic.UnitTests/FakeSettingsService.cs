using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.UnitTests;

public class FakeSettingsService : ISettingsService
{
    public BillingSettingsDto Billing { get; set; } =
        new(0.16m, "MXN", "$", "FAC", new[] { "Efectivo", "Tarjeta", "Transferencia" }, string.Empty);

    public ClinicSettingsDto Clinic { get; set; } =
        new("VetClinic", string.Empty, string.Empty, "RUC", string.Empty, string.Empty, string.Empty,
            null, string.Empty, string.Empty);

    public AgendaSettingsDto Agenda { get; set; } =
        new("08:00", "20:00", 30, 30, 90, new[] { "Sunday" });

    public InventorySettingsDto Inventory { get; set; } =
        new(30, Array.Empty<string>(), new[] { "unidad" }, 5);

    public SecuritySettingsDto Security { get; set; } =
        new(6, true, true, 4, 12, 5);

    public NotificationSettingsDto Notifications { get; set; } =
        new(false, string.Empty, 587, string.Empty, false, true, "VetClinic", string.Empty, new[] { "Email" }, 3);

    public Task<ClinicSettingsDto> GetClinicAsync(CancellationToken ct = default) => Task.FromResult(Clinic);
    public Task<ClinicSettingsDto> SaveClinicAsync(ClinicSettingsDto dto, CancellationToken ct = default)
    {
        Clinic = dto;
        return Task.FromResult(dto);
    }

    public Task<BillingSettingsDto> GetBillingAsync(CancellationToken ct = default) => Task.FromResult(Billing);
    public Task<BillingSettingsDto> SaveBillingAsync(BillingSettingsDto dto, CancellationToken ct = default)
    {
        Billing = dto;
        return Task.FromResult(dto);
    }

    public Task<AgendaSettingsDto> GetAgendaAsync(CancellationToken ct = default) => Task.FromResult(Agenda);
    public Task<AgendaSettingsDto> SaveAgendaAsync(AgendaSettingsDto dto, CancellationToken ct = default)
    {
        Agenda = dto;
        return Task.FromResult(dto);
    }

    public TimeZoneInfo GetAgendaTimeZone() => TimeZoneInfo.Utc;

    public Task<InventorySettingsDto> GetInventoryAsync(CancellationToken ct = default) => Task.FromResult(Inventory);
    public Task<InventorySettingsDto> SaveInventoryAsync(InventorySettingsDto dto, CancellationToken ct = default)
    {
        Inventory = dto;
        return Task.FromResult(dto);
    }

    public Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken ct = default) => Task.FromResult(Security);
    public Task<SecuritySettingsDto> SaveSecurityAsync(SecuritySettingsDto dto, CancellationToken ct = default)
    {
        Security = dto;
        return Task.FromResult(dto);
    }

    public Task<NotificationSettingsDto> GetNotificationsAsync(CancellationToken ct = default) => Task.FromResult(Notifications);
    public Task<NotificationSettingsDto> SaveNotificationsAsync(UpdateNotificationsRequest request, CancellationToken ct = default)
    {
        Notifications = new NotificationSettingsDto(
            request.Enabled, request.SmtpHost, request.SmtpPort, request.SmtpUser,
            !string.IsNullOrEmpty(request.SmtpPassword), request.UseTls, request.FromName, request.FromEmail,
            request.Channels, request.VaccineReminderDaysBefore);
        return Task.FromResult(Notifications);
    }

    public Task<ClinicSettingsDto> GetPublicAsync(CancellationToken ct = default) => Task.FromResult(Clinic);
}
