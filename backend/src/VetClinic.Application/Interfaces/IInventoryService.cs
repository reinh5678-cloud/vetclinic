using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItemDto>> ListAsync(InventoryQuery? query = null, CancellationToken ct = default);
    Task<InventoryItemDto?> GetByIdAsync(int itemId, CancellationToken ct = default);
    Task<InventoryItemDto?> UpdateAsync(int itemId, UpdateInventoryItemRequest request, CancellationToken ct = default);
    Task<InventoryItemDto?> SetStatusAsync(int itemId, UpdateItemStatusRequest request, CancellationToken ct = default);
    Task<InventoryItemDto?> UpdateStockAsync(int itemId, UpdateStockRequest request, CancellationToken ct = default);
    Task<ProductLotDto?> AddLotAsync(int itemId, AddLotRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ProductLotDto>> ListLotsAsync(int itemId, bool includeInactive = false, CancellationToken ct = default);
    Task<bool> DeactivateLotAsync(int lotId, CancellationToken ct = default);
    Task<bool> ReactivateLotAsync(int lotId, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovementDto>> ListMovementsAsync(int itemId, CancellationToken ct = default);
    Task<IReadOnlyList<ExpiringLotDto>> ListExpiringLotsAsync(int days = 30, CancellationToken ct = default);
}
