using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Serialization;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service) => _service = service;

    [HttpGet("dashboard")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from == default) from = DateTime.UtcNow.AddMonths(-1);
        if (to == default) to = DateTime.UtcNow;
        return Ok(await _service.GetDashboardAsync(UtcDateTimeConverter.ToUtc(from), UtcDateTimeConverter.ToUtc(to), ct));
    }

    [HttpGet("revenues")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Revenues([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from == default) from = DateTime.UtcNow.AddYears(-1);
        if (to == default) to = DateTime.UtcNow;
        return Ok(await _service.GetRevenuesAsync(UtcDateTimeConverter.ToUtc(from), UtcDateTimeConverter.ToUtc(to), ct));
    }

    [HttpGet("retention")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Retention(CancellationToken ct) =>
        Ok(await _service.GetClientRetentionAsync(ct));

    [HttpGet("hospitalizations")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Hospitalizations([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (from == default) from = DateTime.UtcNow.AddDays(-29);
        if (to == default) to = DateTime.UtcNow;
        return Ok(await _service.GetHospitalizationTrendAsync(UtcDateTimeConverter.ToUtc(from), UtcDateTimeConverter.ToUtc(to), ct));
    }

    [HttpGet("operations")]
    public async Task<IActionResult> Operations(CancellationToken ct) =>
        Ok(await _service.GetOperationsAsync(ct));

    [HttpGet("low-stock")]
    [Authorize(Roles = "Administrador,Veterinario")]
    public async Task<IActionResult> LowStock(CancellationToken ct) =>
        Ok(await _service.GetLowStockAsync(ct));
}
