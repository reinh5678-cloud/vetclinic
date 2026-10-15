using Microsoft.EntityFrameworkCore;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public static class Fefo
{
    public record ConsumeResult(bool HasRemaining);

    public static async Task ConsumeAsync(
        InventoryItem item,
        IRepository<ProductLot> lotsRepo,
        IRepository<StockMovement> movementsRepo,
        decimal quantity,
        StockMovementType movementType,
        string description,
        int? performedById,
        CancellationToken ct)
    {
        if (item.StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Stock insuficiente para «{item.Name}» (disponible: {item.StockQuantity:F3}, requerido: {quantity:F3})");
        }

        var lots = await lotsRepo.Query()
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
            lotsRepo.Update(lot);

            await movementsRepo.AddAsync(new StockMovement
            {
                InventoryItemId = item.Id,
                ProductLotId = lot.Id,
                Type = movementType,
                Quantity = -take,
                Description = description + (string.IsNullOrWhiteSpace(lot.LotNumber) ? "" : $" (lote {lot.LotNumber})"),
                PerformedById = performedById
            }, ct);
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException($"Stock insuficiente para «{item.Name}» (faltan {remaining:F3} disponibles en lotes activos)");
        }

        item.StockQuantity -= quantity;
    }
}
