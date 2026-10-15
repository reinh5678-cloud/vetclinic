namespace VetClinic.Application.DTOs;

public record ClinicSettingsDto(
    string Name,
    string LegalName,
    string TaxId,
    string TaxIdType,
    string Address,
    string Phone,
    string Email,
    string? LogoDataUrl,
    string InvoiceHeader,
    string InvoiceFooter);

public record BillingSettingsDto(
    decimal TaxRate,
    string Currency,
    string CurrencySymbol,
    string InvoicePrefix,
    string[] PaymentMethods,
    string LegalText);

public record AgendaSettingsDto(
    string OpeningTime,
    string ClosingTime,
    int SlotMinutes,
    int DefaultDurationMinutes,
    int MaxAdvanceDays,
    string[] ClosedWeekdays);

public record InventorySettingsDto(
    int ExpiringSoonDays,
    string[] Categories,
    string[] Units,
    int DefaultMinStock);

public record SecuritySettingsDto(
    int PasswordMinLength,
    bool RequireLetter,
    bool RequireDigit,
    int PinLength,
    int SessionHours,
    int MaxFailedAttempts);

/// <summary>Configuración de notificaciones tal como se devuelve al cliente (contraseña enmascarada).</summary>
public record NotificationSettingsDto(
    bool Enabled,
    string SmtpHost,
    int SmtpPort,
    string SmtpUser,
    bool SmtpPasswordSet,
    bool UseTls,
    string FromName,
    string FromEmail,
    string[] Channels,
    int VaccineReminderDaysBefore);

/// <summary>Entrada para guardar notificaciones. SmtpPassword null/vacío conserva la contraseña actual.</summary>
public record UpdateNotificationsRequest(
    bool Enabled,
    string SmtpHost,
    int SmtpPort,
    string SmtpUser,
    string? SmtpPassword,
    bool UseTls,
    string FromName,
    string FromEmail,
    string[] Channels,
    int VaccineReminderDaysBefore);

public record SystemInfoDto(
    string Environment,
    string Version,
    string Database,
    string AttachmentsPath,
    int MaxAttachmentMb);
