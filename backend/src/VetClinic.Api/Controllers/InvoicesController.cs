using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.CreateAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _service.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var invoice = await _service.GetByIdAsync(id, ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateInvoiceStatusRequest request, CancellationToken ct)
    {
        try
        {
            var invoice = await _service.UpdateStatusAsync(id, request, ct);
            return invoice is null ? NotFound() : Ok(invoice);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}