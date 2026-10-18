using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Authorization;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service) => _service = service;

    [HttpPost]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> Create(CreateInventoryItemRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAsync(request, ct));

    [HttpGet]
    [RequirePermission(Permissions.InventoryView)]
    public async Task<IActionResult> List(
        string? search,
        string? category,
        bool lowOnly = false,
        bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await _service.ListAsync(new InventoryQuery(search, category, lowOnly, includeInactive), ct));

    [HttpGet("lots/expiring")]
    [RequirePermission(Permissions.InventoryView)]
    public async Task<IActionResult> ListExpiringLots(int days = 30, CancellationToken ct = default) =>
        Ok(await _service.ListExpiringLotsAsync(days, ct));

    [HttpGet("movements")]
    [RequirePermission(Permissions.InventoryView)]
    public async Task<IActionResult> ListKardex(
        int? itemId,
        string? type,
        DateTime? from,
        DateTime? to,
        CancellationToken ct) =>
        Ok(await _service.ListKardexAsync(new KardexQuery(itemId, type, from, to), ct));

    [HttpGet("{id:int}")]
    [RequirePermission(Permissions.InventoryView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("{id:int}")]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> Update(int id, UpdateInventoryItemRequest request, CancellationToken ct)
    {
        var item = await _service.UpdateAsync(id, request, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("{id:int}/status")]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> SetStatus(int id, UpdateItemStatusRequest request, CancellationToken ct)
    {
        var item = await _service.SetStatusAsync(id, request, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPatch("{id:int}/stock")]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> UpdateStock(int id, UpdateStockRequest request, CancellationToken ct)
    {
        var item = await _service.UpdateStockAsync(id, request, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:int}/lots")]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> AddLot(int id, AddLotRequest request, CancellationToken ct)
    {
        var lot = await _service.AddLotAsync(id, request, ct);
        return lot is null ? NotFound() : Ok(lot);
    }

    [HttpGet("{id:int}/lots")]
    [RequirePermission(Permissions.InventoryView)]
    public async Task<IActionResult> ListLots(int id, bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await _service.ListLotsAsync(id, includeInactive, ct));

    [HttpGet("{id:int}/movements")]
    [RequirePermission(Permissions.InventoryView)]
    public async Task<IActionResult> ListMovements(int id, CancellationToken ct) =>
        Ok(await _service.ListMovementsAsync(id, ct));

    [HttpDelete("lots/{lotId:int}")]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> DeactivateLot(int lotId, CancellationToken ct) =>
        await _service.DeactivateLotAsync(lotId, ct) ? Ok() : NotFound();

    [HttpPatch("lots/{lotId:int}/activate")]
    [RequirePermission(Permissions.InventoryManage)]
    public async Task<IActionResult> ReactivateLot(int lotId, CancellationToken ct) =>
        await _service.ReactivateLotAsync(lotId, ct) ? Ok() : NotFound();
}
