using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class InventoryService : IInventoryService
{
    private const int MaxListSize = 300;
    private const int MaxMovements = 200;

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
        if (r is null) throw new ArgumentException("Datos del artículo requeridos");

        var name = Normalize(r.Name, "El nombre");
        var category = Normalize(r.Category, "La categoría");
        var code = Normalize(r.Code, "El código");
        var unit = Normalize(r.Unit, "La unidad");
        ValidateNumbers(r.MinStock, r.UnitPrice);

        var exists = await _items.Query().AnyAsync(i => i.Code == code, ct);
        if (exists)
            throw new InvalidOperationException("Ya existe un artículo con ese código");

        var item = new InventoryItem
        {
            Name = name,
            Category = category,
            Code = code,
            Unit = unit,
            StockQuantity = 0,
            MinStock = r.MinStock,
            UnitPrice = r.UnitPrice,
            Supplier = r.Supplier?.Trim() ?? string.Empty,
            IsFractionable = r.IsFractionable,
            IsActive = true
        };
        await _items.AddAsync(item, ct);
        await _items.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> ListAsync(InventoryQuery? query = null, CancellationToken ct = default)
    {
        query ??= new InventoryQuery();
        var q = _items.Query().AsNoTracking();

        if (!query.IncludeInactive)
            q = q.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(i => i.Name.ToLower().Contains(term) || i.Code.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var category = query.Category.Trim();
            q = q.Where(i => i.Category == category);
        }

        if (query.LowOnly)
            q = q.Where(i => i.StockQuantity <= i.MinStock);

        var items = await q.OrderBy(i => i.Name).Take(MaxListSize).ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    public async Task<InventoryItemDto?> GetByIdAsync(int itemId, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        return item is null ? null : Map(item);
    }

    public async Task<InventoryItemDto?> UpdateAsync(int itemId, UpdateInventoryItemRequest r, CancellationToken ct = default)
    {
        if (r is null) throw new ArgumentException("Datos del artículo requeridos");

        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return null;

        var name = Normalize(r.Name, "El nombre");
        var category = Normalize(r.Category, "La categoría");
        var code = Normalize(r.Code, "El código");
        var unit = Normalize(r.Unit, "La unidad");
        ValidateNumbers(r.MinStock, r.UnitPrice);

        var duplicate = await _items.Query().AnyAsync(i => i.Code == code && i.Id != itemId, ct);
        if (duplicate)
            throw new InvalidOperationException("Ya existe otro artículo con ese código");

        item.Name = name;
        item.Category = category;
        item.Code = code;
        item.Unit = unit;
        item.MinStock = r.MinStock;
        item.UnitPrice = r.UnitPrice;
        item.Supplier = r.Supplier?.Trim() ?? string.Empty;
        item.IsFractionable = r.IsFractionable;
        _items.Update(item);
        await _items.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<InventoryItemDto?> SetStatusAsync(int itemId, UpdateItemStatusRequest r, CancellationToken ct = default)
    {
        if (r is null) throw new ArgumentException("Datos de estado requeridos");

        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return null;

        item.IsActive = r.IsActive;
        _items.Update(item);
        await _items.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<InventoryItemDto?> UpdateStockAsync(int itemId, UpdateStockRequest r, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return null;

        if (r.Quantity == 0)
            throw new ArgumentException("La cantidad del ajuste debe ser distinta de cero");

        var description = string.IsNullOrWhiteSpace(r.Description) ? "Ajuste manual" : r.Description.Trim();

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            if (r.Quantity < 0)
            {
                await Fefo.ConsumeAsync(item, _lots, _movements, Math.Abs(r.Quantity), StockMovementType.Ajuste, description, _currentUser.UserId, ct);
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
                    Description = description,
                    PerformedById = _currentUser.UserId
                }, ct);
                item.StockQuantity += r.Quantity;
            }

            _items.Update(item);
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

    public async Task<ProductLotDto?> AddLotAsync(int itemId, AddLotRequest r, CancellationToken ct = default)
    {
        var item = await _items.GetByIdAsync(itemId, ct);
        if (item is null) return null;

        var lotNumber = Normalize(r.LotNumber, "El número de lote");
        if (r.Quantity <= 0)
            throw new ArgumentException("La cantidad del lote debe ser mayor a cero");

        var lot = new ProductLot
        {
            InventoryItemId = itemId,
            LotNumber = lotNumber,
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
                Description = string.IsNullOrWhiteSpace(r.Description) ? "Entrada por compra" : r.Description.Trim(),
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

    public async Task<IReadOnlyList<ProductLotDto>> ListLotsAsync(int itemId, bool includeInactive = false, CancellationToken ct = default)
    {
        var q = _lots.Query().AsNoTracking().Where(l => l.InventoryItemId == itemId);
        if (!includeInactive)
            q = q.Where(l => l.IsActive);

        var lots = await q.OrderBy(l => l.ExpirationDate).ThenBy(l => l.Id).ToListAsync(ct);
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
            _lots.Update(lot);

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
                Description = $"Baja de lote {lot.LotNumber}: {released:F3} unidades retiradas del stock",
                PerformedById = _currentUser.UserId
            }, ct);

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

    public async Task<bool> ReactivateLotAsync(int lotId, CancellationToken ct = default)
    {
        var lot = await _lots.GetByIdAsync(lotId, ct);
        if (lot is null || lot.IsActive) return false;

        var item = await _items.GetByIdAsync(lot.InventoryItemId, ct);
        var restored = lot.AvailableQuantity;

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            lot.IsActive = true;
            _lots.Update(lot);

            if (item is not null)
            {
                item.StockQuantity += restored;
                _items.Update(item);
            }

            await _movements.AddAsync(new StockMovement
            {
                InventoryItemId = lot.InventoryItemId,
                ProductLotId = lot.Id,
                Type = StockMovementType.Ajuste,
                Quantity = restored,
                Description = $"Reactivación de lote {lot.LotNumber}: {restored:F3} unidades devueltas al stock",
                PerformedById = _currentUser.UserId
            }, ct);

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

    public async Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(int itemId, CancellationToken ct = default)
    {
        var movements = await _movements.Query()
            .AsNoTracking()
            .Include(m => m.ProductLot)
            .Include(m => m.PerformedBy)
            .Where(m => m.InventoryItemId == itemId)
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.Id)
            .Take(MaxMovements)
            .ToListAsync(ct);

        return movements.Select(m => new StockMovementDto(
            m.Id,
            m.Type.ToString(),
            m.Quantity,
            m.Description,
            m.MovementDate,
            m.ProductLot?.LotNumber,
            m.PerformedBy?.FullName)).ToList();
    }

    public async Task<IReadOnlyList<KardexEntryDto>> ListKardexAsync(KardexQuery? query = null, CancellationToken ct = default)
    {
        var movements = _movements.Query()
            .AsNoTracking()
            .Include(m => m.InventoryItem)
            .Include(m => m.ProductLot)
            .Include(m => m.PerformedBy)
            .AsQueryable();

        if (query?.ItemId is int itemId)
            movements = movements.Where(m => m.InventoryItemId == itemId);

        if (!string.IsNullOrWhiteSpace(query?.Type)
            && Enum.TryParse<StockMovementType>(query.Type, ignoreCase: true, out var type))
            movements = movements.Where(m => m.Type == type);

        if (query?.From is DateTime from)
            movements = movements.Where(m => m.MovementDate >= ToUtc(from));

        if (query?.To is DateTime to)
            movements = movements.Where(m => m.MovementDate <= ToUtc(to).AddDays(1).AddTicks(-1));

        var rows = await movements
            .OrderByDescending(m => m.MovementDate)
            .ThenByDescending(m => m.Id)
            .Take(MaxMovements)
            .ToListAsync(ct);

        return rows.Select(m => new KardexEntryDto(
            m.Id,
            m.InventoryItemId,
            m.InventoryItem?.Name ?? "—",
            m.InventoryItem?.Code ?? "—",
            m.Type.ToString(),
            m.Quantity,
            m.Description,
            m.MovementDate,
            m.ProductLot?.LotNumber,
            m.PerformedBy?.FullName)).ToList();
    }

    public async Task<IReadOnlyList<ExpiringLotDto>> ListExpiringLotsAsync(int days = 30, CancellationToken ct = default)
    {
        if (days < 0) days = 0;
        var today = DateTime.UtcNow.Date;
        var limit = today.AddDays(days);

        var lots = await _lots.Query()
            .AsNoTracking()
            .Include(l => l.InventoryItem)
            .Where(l => l.IsActive && l.AvailableQuantity > 0 && l.ExpirationDate != null && l.ExpirationDate.Value.Date <= limit)
            .OrderBy(l => l.ExpirationDate)
            .ThenBy(l => l.Id)
            .ToListAsync(ct);

        return lots.Select(l => new ExpiringLotDto(
            l.Id,
            l.InventoryItemId,
            l.InventoryItem?.Name ?? "—",
            l.InventoryItem?.Code ?? "—",
            l.LotNumber,
            l.ExpirationDate,
            l.AvailableQuantity,
            l.ExpirationDate!.Value.Date < today)).ToList();
    }

    private static string Normalize(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{label} es obligatorio");
        return value.Trim();
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static void ValidateNumbers(decimal minStock, decimal unitPrice)
    {
        if (minStock < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo");
        if (unitPrice < 0)
            throw new ArgumentException("El precio unitario no puede ser negativo");
    }

    private static InventoryItemDto Map(InventoryItem i) => new(
        i.Id, i.Name, i.Category, i.Code, i.Unit, i.StockQuantity, i.MinStock, i.UnitPrice,
        i.Supplier, i.StockQuantity <= i.MinStock, i.IsFractionable, i.IsActive);

    private static ProductLotDto Map(ProductLot l) => new(
        l.Id, l.InventoryItemId, l.LotNumber, l.ExpirationDate, l.AvailableQuantity,
        l.ExpirationDate.HasValue && l.ExpirationDate.Value.Date <= DateTime.UtcNow.AddDays(30) && l.AvailableQuantity > 0,
        l.IsActive);
}
