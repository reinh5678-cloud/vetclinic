using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IRepository<AppSetting> _settings;
    private readonly IConfiguration _configuration;

    public SettingsService(IRepository<AppSetting> settings, IConfiguration configuration)
    {
        _settings = settings;
        _configuration = configuration;
    }

    // ---------- Clínica ----------

    public Task<ClinicSettingsDto> GetClinicAsync(CancellationToken ct = default) =>
        GetSectionAsync(AppSetting.Sections.Clinic, DefaultClinic, ct);

    public Task<ClinicSettingsDto> SaveClinicAsync(ClinicSettingsDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("El nombre de la clínica es obligatorio.");
        return SaveSectionAsync(AppSetting.Sections.Clinic, dto, ct);
    }

    // ---------- Facturación ----------

    public Task<BillingSettingsDto> GetBillingAsync(CancellationToken ct = default) =>
        GetSectionAsync(AppSetting.Sections.Billing, DefaultBilling, ct);

    public Task<BillingSettingsDto> SaveBillingAsync(BillingSettingsDto dto, CancellationToken ct = default)
    {
        if (dto.TaxRate < 0m || dto.TaxRate > 1m)
            throw new ArgumentException("El IVA debe estar entre 0 y 1 (por ejemplo, 0.16 = 16%).");
        if (dto.PaymentMethods is null || dto.PaymentMethods.Length == 0)
            throw new ArgumentException("Debe indicar al menos un medio de pago.");
        if (string.IsNullOrWhiteSpace(dto.InvoicePrefix))
            throw new ArgumentException("El prefijo de numeración es obligatorio.");

        var cleaned = dto with
        {
            PaymentMethods = dto.PaymentMethods.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).ToArray(),
            InvoicePrefix = dto.InvoicePrefix.Trim()
        };
        return SaveSectionAsync(AppSetting.Sections.Billing, cleaned, ct);
    }

    // ---------- Agenda ----------

    public Task<AgendaSettingsDto> GetAgendaAsync(CancellationToken ct = default) =>
        GetSectionAsync(AppSetting.Sections.Agenda, DefaultAgenda, ct);

    public Task<AgendaSettingsDto> SaveAgendaAsync(AgendaSettingsDto dto, CancellationToken ct = default)
    {
        if (!TimeSpan.TryParse(dto.OpeningTime, CultureInfo.InvariantCulture, out _))
            throw new ArgumentException("La hora de apertura no es válida (formato HH:mm).");
        if (!TimeSpan.TryParse(dto.ClosingTime, CultureInfo.InvariantCulture, out _))
            throw new ArgumentException("La hora de cierre no es válida (formato HH:mm).");
        if (dto.SlotMinutes < 5 || dto.SlotMinutes > 240)
            throw new ArgumentException("El intervalo de agenda debe estar entre 5 y 240 minutos.");
        if (dto.DefaultDurationMinutes < dto.SlotMinutes)
            throw new ArgumentException("La duración por defecto no puede ser menor que el intervalo de agenda.");
        if (dto.MaxAdvanceDays < 1 || dto.MaxAdvanceDays > 730)
            throw new ArgumentException("Los días de antelación deben estar entre 1 y 730.");
        return SaveSectionAsync(AppSetting.Sections.Agenda, dto, ct);
    }

    // ---------- Inventario ----------

    public Task<InventorySettingsDto> GetInventoryAsync(CancellationToken ct = default) =>
        GetSectionAsync(AppSetting.Sections.Inventory, DefaultInventory, ct);

    public Task<InventorySettingsDto> SaveInventoryAsync(InventorySettingsDto dto, CancellationToken ct = default)
    {
        if (dto.ExpiringSoonDays < 1 || dto.ExpiringSoonDays > 365)
            throw new ArgumentException("Los días de aviso de vencimiento deben estar entre 1 y 365.");
        if (dto.DefaultMinStock < 0)
            throw new ArgumentException("El stock mínimo por defecto no puede ser negativo.");
        return SaveSectionAsync(AppSetting.Sections.Inventory, dto, ct);
    }

    // ---------- Seguridad ----------

    public Task<SecuritySettingsDto> GetSecurityAsync(CancellationToken ct = default) =>
        GetSectionAsync(AppSetting.Sections.Security, DefaultSecurity, ct);

    public Task<SecuritySettingsDto> SaveSecurityAsync(SecuritySettingsDto dto, CancellationToken ct = default)
    {
        if (dto.PasswordMinLength < 4 || dto.PasswordMinLength > 64)
            throw new ArgumentException("La longitud mínima de contraseña debe estar entre 4 y 64.");
        if (dto.PinLength < 4 || dto.PinLength > 12)
            throw new ArgumentException("La longitud del PIN debe estar entre 4 y 12.");
        if (dto.SessionHours < 1 || dto.SessionHours > 168)
            throw new ArgumentException("La duración de sesión debe estar entre 1 y 168 horas.");
        if (dto.MaxFailedAttempts < 1 || dto.MaxFailedAttempts > 20)
            throw new ArgumentException("Los intentos fallidos permitidos deben estar entre 1 y 20.");
        return SaveSectionAsync(AppSetting.Sections.Security, dto, ct);
    }

    // ---------- Notificaciones ----------

    public async Task<NotificationSettingsDto> GetNotificationsAsync(CancellationToken ct = default)
    {
        var store = await GetSectionAsync(AppSetting.Sections.Notifications, DefaultNotificationStore, ct);
        return new NotificationSettingsDto(
            store.Enabled, store.SmtpHost, store.SmtpPort, store.SmtpUser,
            !string.IsNullOrEmpty(store.SmtpPassword), store.UseTls, store.FromName, store.FromEmail,
            store.Channels, store.VaccineReminderDaysBefore);
    }

    public async Task<NotificationSettingsDto> SaveNotificationsAsync(UpdateNotificationsRequest request, CancellationToken ct = default)
    {
        if (request.VaccineReminderDaysBefore < 0 || request.VaccineReminderDaysBefore > 60)
            throw new ArgumentException("Los días de recordatorio de vacunas deben estar entre 0 y 60.");
        if (request.Enabled && string.IsNullOrWhiteSpace(request.SmtpHost))
            throw new ArgumentException("Debe indicar el servidor SMTP para habilitar las notificaciones.");
        if (request.SmtpPort < 1 || request.SmtpPort > 65535)
            throw new ArgumentException("El puerto SMTP no es válido.");

        var existing = await GetSectionAsync(AppSetting.Sections.Notifications, DefaultNotificationStore, ct);
        var password = string.IsNullOrEmpty(request.SmtpPassword) ? existing.SmtpPassword : request.SmtpPassword;

        var store = new NotificationStore(
            request.Enabled, request.SmtpHost ?? string.Empty, request.SmtpPort, request.SmtpUser ?? string.Empty,
            password ?? string.Empty, request.UseTls, request.FromName ?? string.Empty, request.FromEmail ?? string.Empty,
            request.Channels ?? Array.Empty<string>(), request.VaccineReminderDaysBefore);

        await SaveSectionAsync(AppSetting.Sections.Notifications, store, ct);
        return await GetNotificationsAsync(ct);
    }

    public async Task<ClinicSettingsDto> GetPublicAsync(CancellationToken ct = default) =>
        await GetClinicAsync(ct);

    // ---------- Infraestructura ----------

    private async Task<T> GetSectionAsync<T>(string section, Func<T> defaults, CancellationToken ct)
    {
        var row = await _settings.Query().AsNoTracking().FirstOrDefaultAsync(s => s.Section == section, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.Json))
            return defaults();

        try
        {
            return JsonSerializer.Deserialize<T>(row.Json, JsonOptions) ?? defaults();
        }
        catch (JsonException)
        {
            return defaults();
        }
    }

    private async Task<T> SaveSectionAsync<T>(string section, T value, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var row = await _settings.Query().FirstOrDefaultAsync(s => s.Section == section, ct);
        if (row is null)
            await _settings.AddAsync(new AppSetting { Section = section, Json = json }, ct);
        else
        {
            row.Json = json;
            _settings.Update(row);
        }

        await _settings.SaveChangesAsync(ct);
        return value;
    }

    private decimal ConfigTaxRate()
    {
        var raw = _configuration["Billing:TaxRate"];
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate >= 0m
            ? rate
            : 0.16m;
    }

    private int ConfigSessionHours()
    {
        var raw = _configuration["Jwt:ExpirationHours"];
        return int.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var hours) && hours > 0
            ? hours
            : 12;
    }

    private ClinicSettingsDto DefaultClinic() => new(
        "VetClinic", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        null, "VetClinic - Servicios Veterinarios", "Gracias por su preferencia.");

    private BillingSettingsDto DefaultBilling() => new(
        ConfigTaxRate(), "MXN", "$", "FAC",
        new[] { "Efectivo", "Tarjeta", "Transferencia" }, string.Empty);

    private static AgendaSettingsDto DefaultAgenda() => new(
        "08:00", "20:00", 30, 30, 90, new[] { "Sunday" });

    private static InventorySettingsDto DefaultInventory() => new(
        30, Array.Empty<string>(), new[] { "unidad", "caja", "ml", "mg", "kg" }, 5);

    private SecuritySettingsDto DefaultSecurity() => new(
        6, true, true, 4, ConfigSessionHours(), 5);

    private static NotificationStore DefaultNotificationStore() => new(
        false, string.Empty, 587, string.Empty, string.Empty, true, "VetClinic", string.Empty,
        new[] { "Email" }, 3);

    private record NotificationStore(
        bool Enabled,
        string SmtpHost,
        int SmtpPort,
        string SmtpUser,
        string SmtpPassword,
        bool UseTls,
        string FromName,
        string FromEmail,
        string[] Channels,
        int VaccineReminderDaysBefore);
}
