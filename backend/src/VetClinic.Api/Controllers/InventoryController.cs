using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Veterinario,Administrador")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateInventoryItemRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAsync(request, ct));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _service.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, UpdateStockRequest request, CancellationToken ct)
    {
        var item = await _service.UpdateStockAsync(id, request, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:int}/lots")]
    public async Task<IActionResult> AddLot(int id, AddLotRequest request, CancellationToken ct)
    {
        var lot = await _service.AddLotAsync(id, request, ct);
        return lot is null ? NotFound() : Ok(lot);
    }

    [HttpGet("{id:int}/lots")]
    public async Task<IActionResult> ListLots(int id, CancellationToken ct) =>
        Ok(await _service.ListLotsAsync(id, ct));

    [HttpDelete("lots/{lotId:int}")]
    public async Task<IActionResult> DeactivateLot(int lotId, CancellationToken ct) =>
        await _service.DeactivateLotAsync(lotId, ct) ? Ok() : NotFound();
}