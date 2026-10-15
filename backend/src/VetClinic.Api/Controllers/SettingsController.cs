using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Administrador")]
public class SettingsController : ControllerBase
{
    private const int MaxAttachmentMb = 20;
    private const string DefaultAttachmentsPath = "uploads/attachments";

    private readonly ISettingsService _settings;
    private readonly IInventoryService _inventory;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SettingsController(
        ISettingsService settings,
        IInventoryService inventory,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _settings = settings;
        _inventory = inventory;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> Public(CancellationToken ct)
    {
        var clinic = await _settings.GetPublicAsync(ct);
        return Ok(new { name = clinic.Name, logoDataUrl = clinic.LogoDataUrl });
    }

    [HttpGet("clinic")]
    public async Task<IActionResult> GetClinic(CancellationToken ct)
        => Ok(await _settings.GetClinicAsync(ct));

    [HttpPut("clinic")]
    public async Task<IActionResult> UpdateClinic(ClinicSettingsDto request, CancellationToken ct)
        => Ok(await _settings.SaveClinicAsync(request, ct));

    [HttpGet("billing")]
    public async Task<IActionResult> GetBilling(CancellationToken ct)
        => Ok(await _settings.GetBillingAsync(ct));

    [HttpPut("billing")]
    public async Task<IActionResult> UpdateBilling(BillingSettingsDto request, CancellationToken ct)
        => Ok(await _settings.SaveBillingAsync(request, ct));

    [HttpGet("agenda")]
    public async Task<IActionResult> GetAgenda(CancellationToken ct)
        => Ok(await _settings.GetAgendaAsync(ct));

    [HttpPut("agenda")]
    public async Task<IActionResult> UpdateAgenda(AgendaSettingsDto request, CancellationToken ct)
        => Ok(await _settings.SaveAgendaAsync(request, ct));

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory(CancellationToken ct)
        => Ok(await _settings.GetInventoryAsync(ct));

    [HttpPut("inventory")]
    public async Task<IActionResult> UpdateInventory(InventorySettingsDto request, CancellationToken ct)
        => Ok(await _settings.SaveInventoryAsync(request, ct));

    [HttpGet("inventory/usage")]
    public async Task<IActionResult> InventoryUsage(CancellationToken ct)
    {
        var items = await _inventory.ListAsync(new InventoryQuery(IncludeInactive: true), ct);
        var categories = items.GroupBy(i => i.Category).ToDictionary(g => g.Key, g => g.Count());
        var units = items.GroupBy(i => i.Unit).ToDictionary(g => g.Key, g => g.Count());
        return Ok(new { categories, units });
    }

    [HttpGet("security")]
    public async Task<IActionResult> GetSecurity(CancellationToken ct)
        => Ok(await _settings.GetSecurityAsync(ct));

    [HttpPut("security")]
    public async Task<IActionResult> UpdateSecurity(SecuritySettingsDto request, CancellationToken ct)
        => Ok(await _settings.SaveSecurityAsync(request, ct));

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
        => Ok(await _settings.GetNotificationsAsync(ct));

    [HttpPut("notifications")]
    public async Task<IActionResult> UpdateNotifications(UpdateNotificationsRequest request, CancellationToken ct)
        => Ok(await _settings.SaveNotificationsAsync(request, ct));

    [HttpPost("notifications/test")]
    public async Task<IActionResult> TestNotifications(CancellationToken ct)
    {
        var current = await _settings.GetNotificationsAsync(ct);
        if (!current.Enabled)
            return Ok(new { ok = false, message = "Las notificaciones están deshabilitadas." });
        if (string.IsNullOrWhiteSpace(current.SmtpHost))
            return Ok(new { ok = false, message = "Falta configurar el servidor SMTP." });

        return Ok(new
        {
            ok = true,
            message = $"Configuración SMTP válida ({current.SmtpHost}:{current.SmtpPort}). " +
                      "El envío real se activará cuando se implemente el proveedor de correo."
        });
    }

    [HttpGet("system")]
    public IActionResult SystemInfo()
    {
        var attachmentsPath = _configuration["Storage:AttachmentsPath"] ?? DefaultAttachmentsPath;
        var version = typeof(SettingsController).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        var database = string.IsNullOrWhiteSpace(_configuration.GetConnectionString("DefaultConnection"))
            ? "no configurada"
            : "PostgreSQL";

        return Ok(new SystemInfoDto(
            _environment.EnvironmentName,
            version,
            database,
            attachmentsPath,
            MaxAttachmentMb));
    }
}
