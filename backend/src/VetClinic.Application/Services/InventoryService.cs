using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IRepository<InventoryItem> _items;
    private readonly IRepository<ProductLot> _lots;
    private readonly IRepository<StockMovement> _movements;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public InventoryService(
        IRepository<InventoryItem> items,
        IRepository<ProductLot> lots,
        IRepository<StockMovement> movements,
        IUnitOfWork uow,
        ICurrentUser currentUser)
    {
        _items = items;
        _lots = lots;
        _movements = movements;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest r, CancellationToken ct = default)
    {
        var exists = await _items.Query().AnyAsync(i => i.Code == r.Code, ct);
        if (exists)
            throw new InvalidOperationException("Ya existe un artículo con ese código");

        var item = new InventoryItem
        {
            Name = r.Name,
            Category = r.Category,
            Code = r.Code,
            Unit = r.Unit,
            StockQuantity = 0,
            MinStock = r.MinStock,
            UnitPrice = r.UnitPrice,
            Supplier = r.Supplier,
            IsFractionable = r.IsFractionable
        };
        await _items.AddAsync(item, ct);
        await _items.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> ListAsync(CancellationToken ct = default)
    {
        var items = await _items.Query().OrderBy(i => i.Name).ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<InventoryItemDto?> GetByIdAsync(int itemId, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        return item is null ? null : Map(item);
    }

    public async Task<InventoryItemDto?> UpdateStockAsync(int itemId, UpdateStockRequest r, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return null;

        if (r.Quantity == 0)
            throw new ArgumentException("La cantidad del ajuste debe ser distinta de cero");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            if (r.Quantity < 0)
            {
                await ConsumeStockAsync(item, Math.Abs(r.Quantity), r.Description, ct);
            }
            else
            {
                var lot = new ProductLot
                {
                    InventoryItemId = itemId,
                    LotNumber = $"AJ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..20],
                    ExpirationDate = null,
                    AvailableQuantity = r.Quantity
                };
                await _lots.AddAsync(lot, ct);
                await _movements.AddAsync(new StockMovement
                {
                    InventoryItemId = itemId,
                    ProductLot = lot,
                    Type = StockMovementType.Ajuste,
                    Quantity = r.Quantity,
                    Description = r.Description,
                    PerformedById = _currentUser.UserId
                }, ct);
                item.StockQuantity += r.Quantity;
                _items.Update(item);
            }

            await _items.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return Map(item);
    }

    private async Task ConsumeStockAsync(InventoryItem item, decimal quantity, string description, CancellationToken ct)
    {
        if (item.StockQuantity < quantity)
            throw new InvalidOperationException($"Stock insuficiente para la salida solicitada (disponible: {item.StockQuantity:F3}, solicitado: {quantity:F3})");

        var lots = await _lots.Query()
            .Where(l => l.InventoryItemId == item.Id && l.IsActive && l.AvailableQuantity > 0)
            .ToListAsync(ct);
        lots = lots
            .OrderByDescending(l => l.ExpirationDate.HasValue)
            .ThenBy(l => l.ExpirationDate)
            .ThenBy(l => l.Id)
            .ToList();

        var remaining = quantity;
        foreach (var lot in lots)
        {
            if (remaining <= 0) break;

            var take = Math.Min(lot.AvailableQuantity, remaining);
            lot.AvailableQuantity -= take;
            remaining -= take;
            _lots.Update(lot);
            await _movements.AddAsync(new StockMovement
            {
                InventoryItemId = item.Id,
                ProductLotId = lot.Id,
                Type = StockMovementType.Ajuste,
                Quantity = take,
                Description = description,
                PerformedById = _currentUser.UserId
            }, ct);
        }

        if (remaining > 0)
            throw new InvalidOperationException($"Stock insuficiente para la salida solicitada (faltan {remaining:F3} disponibles en lotes activos)");

        item.StockQuantity -= quantity;
        _items.Update(item);
    }

    public async Task<ProductLotDto?> AddLotAsync(int itemId, AddLotRequest r, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return null;
        if (r.Quantity <= 0)
            throw new InvalidOperationException("La cantidad del lote debe ser mayor a cero");

        var lot = new ProductLot
        {
            InventoryItemId = itemId,
            LotNumber = r.LotNumber,
            ExpirationDate = r.ExpirationDate,
            AvailableQuantity = r.Quantity
        };

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            await _lots.AddAsync(lot, ct);
            await _movements.AddAsync(new StockMovement
            {
                InventoryItemId = itemId,
                ProductLot = lot,
                Type = StockMovementType.EntradaCompra,
                Quantity = r.Quantity,
                Description = r.Description,
                PerformedById = _currentUser.UserId
            }, ct);

            item.StockQuantity += r.Quantity;
            _items.Update(item);
            await _items.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return Map(lot);
    }

    public async Task<IReadOnlyList<ProductLotDto>> ListLotsAsync(int itemId, CancellationToken ct = default)
    {
        var lots = await _lots.Query()
            .Where(l => l.InventoryItemId == itemId && l.IsActive)
            .OrderBy(l => l.ExpirationDate)
            .ToListAsync(ct);
        return lots.Select(Map).ToList();
    }

    public async Task<bool> DeactivateLotAsync(int lotId, CancellationToken ct = default)
    {
        var lot = await _lots.GetByIdAsync(lotId, ct);
        if (lot is null || !lot.IsActive) return false;

        var item = await _items.GetByIdAsync(lot.InventoryItemId, ct);
        var released = lot.AvailableQuantity;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            lot.IsActive = false;
            lot.AvailableQuantity = 0;
            _lots.Update(lot);

            if (released > 0)
            {
                if (item is not null)
                {
                    item.StockQuantity = Math.Max(0, item.StockQuantity - released);
                    _items.Update(item);
                }

                await _movements.AddAsync(new StockMovement
                {
                    InventoryItemId = lot.InventoryItemId,
                    ProductLotId = lot.Id,
                    Type = StockMovementType.Ajuste,
                    Quantity = -released,
                    Description = $"Baja de lote {lot.LotNumber}: {released:F3} unidades liberadas del stock",
                    PerformedById = _currentUser.UserId
                }, ct);
            }

            await _lots.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return true;
    }

    private static InventoryItemDto Map(InventoryItem i) => new(
        i.Id, i.Name, i.Category, i.Code, i.Unit, i.StockQuantity, i.MinStock, i.UnitPrice,
        i.Supplier, i.StockQuantity <= i.MinStock, i.IsFractionable);

    private static ProductLotDto Map(ProductLot l) => new(
        l.Id, l.InventoryItemId, l.LotNumber, l.ExpirationDate, l.AvailableQuantity,
        l.ExpirationDate.HasValue && l.ExpirationDate.Value.Date <= DateTime.UtcNow.AddDays(30) && l.AvailableQuantity > 0);
}