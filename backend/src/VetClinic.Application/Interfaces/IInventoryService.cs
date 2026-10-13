using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InventoryItemDto>> ListAsync(CancellationToken ct = default);
    Task<InventoryItemDto?> UpdateStockAsync(int itemId, UpdateStockRequest request, CancellationToken ct = default);
    Task<InventoryItemDto?> GetByIdAsync(int itemId, CancellationToken ct = default);
    Task<ProductLotDto?> AddLotAsync(int itemId, AddLotRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ProductLotDto>> ListLotsAsync(int itemId, CancellationToken ct = default);
    Task<bool> DeactivateLotAsync(int lotId, CancellationToken ct = default);
}