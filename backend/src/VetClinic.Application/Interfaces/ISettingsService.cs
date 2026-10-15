using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface ISettingsService
{
    Task<ClinicSettingsDto> GetClinicAsync(CancellationToken ct = default);
    Task<ClinicSettingsDto> SaveClinicAsync(ClinicSettingsDto dto, CancellationToken ct = default);

    Task<BillingSettingsDto> GetBillingAsync(CancellationToken ct = default);
    Task<BillingSettingsDto> SaveBillingAsync(BillingSettingsDto dto, CancellationToken ct = default);

    Task<AgendaSettingsDto> GetAgendaAsync(CancellationToken ct = default);
    Task<AgendaSettingsDto> SaveAgendaAsync(AgendaSettingsDto dto, CancellationToken ct = default);

    /// <summary>Zona horaria de la clínica para interpretar las horas de la agenda.</summary>
    TimeZoneInfo GetAgendaTimeZone();

    Task<InventorySettingsDto> GetInventoryAsync(CancellationToken ct = default);
    Task<InventorySettingsDto> SaveInventoryAsync(InventorySettingsDto dto, CancellationToken ct = default);

    Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken ct = default);
    Task<SecuritySettingsDto> SaveSecurityAsync(SecuritySettingsDto dto, CancellationToken ct = default);

    Task<NotificationSettingsDto> GetNotificationsAsync(CancellationToken ct = default);
    Task<NotificationSettingsDto> SaveNotificationsAsync(UpdateNotificationsRequest request, CancellationToken ct = default);

    /// <summary>Datos públicos sin autenticación (branding).</summary>
    Task<ClinicSettingsDto> GetPublicAsync(CancellationToken ct = default);
}
