namespace VetClinic.Application.DTOs;

public record CreateInventoryItemRequest(string Name, string Category, string Code, string Unit, int MinStock, decimal UnitPrice, string Supplier, bool IsFractionable = false);

public record UpdateStockRequest(decimal Quantity, string Description);

public record AddLotRequest(string LotNumber, DateTime? ExpirationDate, decimal Quantity, string Description);

public record ProductLotDto(int Id, int InventoryItemId, string LotNumber, DateTime? ExpirationDate, decimal AvailableQuantity, bool IsExpiringSoon);

public record InventoryItemDto(int Id, string Name, string Category, string Code, string Unit, decimal StockQuantity, decimal MinStock, decimal UnitPrice, string Supplier, bool LowStock, bool IsFractionable);