using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Services;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Administrador")]
public class SettingsController : ControllerBase
{
    private const int MaxAttachmentMb = 20;
    private const int ExpiringSoonDays = 30;
    private const decimal DefaultTaxRate = 0.16m;
    private const double DefaultExpirationHours = 12;
    private const string DefaultAttachmentsPath = "uploads/attachments";

    private readonly IUserService _users;
    private readonly IRoleService _roles;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly LocalSettingsWriter _settingsWriter;

    public SettingsController(
        IUserService users,
        IRoleService roles,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        LocalSettingsWriter settingsWriter)
    {
        _users = users;
        _roles = roles;
        _configuration = configuration;
        _environment = environment;
        _settingsWriter = settingsWriter;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
        => Ok(await _users.ListAsync(ct));

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
        => Ok(await _roles.ListAsync(ct));

    [HttpGet("roles/{id:int}")]
    public async Task<IActionResult> GetRole(int id, CancellationToken ct)
    {
        var role = await _roles.GetAsync(id, ct);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request, CancellationToken ct)
        => Ok(await _roles.CreateAsync(request, ct));

    [HttpPut("roles/{id:int}")]
    public async Task<IActionResult> UpdateRole(int id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _roles.UpdateAsync(id, request, ct);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpGet("permissions")]
    public IActionResult ListPermissions()
        => Ok(Permissions.Catalog.Select(p => new PermissionDto(p.Code, p.Group, p.Label)));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken ct)
        => Ok(await _users.CreateAsync(request, ct));

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _users.UpdateAsync(id, request, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("users/{id:int}/password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken ct)
        => await _users.ResetPasswordAsync(id, request, ct) ? NoContent() : NotFound();

    [HttpPost("users/{id:int}/pin")]
    public async Task<IActionResult> SetPin(int id, SetPinRequest request, CancellationToken ct)
    {
        var user = await _users.SetPinAsync(id, request, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("system")]
    public IActionResult SystemSettings()
    {
        var taxRate = decimal.TryParse(_configuration["Billing:TaxRate"], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedTax)
            ? parsedTax
            : DefaultTaxRate;

        var expirationHours = double.TryParse(_configuration["Jwt:ExpirationHours"], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedHours) && parsedHours > 0
            ? parsedHours
            : DefaultExpirationHours;

        var attachmentsPath = _configuration["Storage:AttachmentsPath"] ?? DefaultAttachmentsPath;
        var version = typeof(SettingsController).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        return Ok(new SystemSettingsDto(
            taxRate,
            expirationHours,
            attachmentsPath,
            MaxAttachmentMb,
            ExpiringSoonDays,
            _environment.EnvironmentName,
            version));
    }

    [HttpPut("billing")]
    public IActionResult UpdateBilling(UpdateBillingRequest request)
    {
        if (request.TaxRate < 0m || request.TaxRate > 1m)
            return BadRequest(new { message = "El IVA debe estar entre 0 y 1 (por ejemplo, 0.16 = 16%)." });

        _settingsWriter.SetBillingTaxRate(request.TaxRate);
        if (_configuration is IConfigurationRoot root)
            root.Reload();

        return Ok(new { taxRate = request.TaxRate });
    }
}
