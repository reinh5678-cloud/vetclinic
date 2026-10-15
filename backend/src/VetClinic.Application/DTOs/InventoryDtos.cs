namespace VetClinic.Application.DTOs;

public record CreateInventoryItemRequest(string Name, string Category, string Code, string Unit, decimal MinStock, decimal UnitPrice, string Supplier, bool IsFractionable = false);

public record UpdateInventoryItemRequest(string Name, string Category, string Code, string Unit, decimal MinStock, decimal UnitPrice, string Supplier, bool IsFractionable);

public record UpdateItemStatusRequest(bool IsActive);

public record InventoryQuery(string? Search = null, string? Category = null, bool LowOnly = false, bool IncludeInactive = false);

public record UpdateStockRequest(decimal Quantity, string Description);

public record AddLotRequest(string LotNumber, DateTime? ExpirationDate, decimal Quantity, string Description);

public record ProductLotDto(int Id, int InventoryItemId, string LotNumber, DateTime? ExpirationDate, decimal AvailableQuantity, bool IsExpiringSoon, bool IsActive);

public record StockMovementDto(int Id, string Type, decimal Quantity, string Description, DateTime MovementDate, string? LotNumber, string? PerformedByName);

public record ExpiringLotDto(int LotId, int InventoryItemId, string ItemName, string ItemCode, string LotNumber, DateTime? ExpirationDate, decimal AvailableQuantity, bool IsExpired);

public record InventoryItemDto(int Id, string Name, string Category, string Code, string Unit, decimal StockQuantity, decimal MinStock, decimal UnitPrice, string Supplier, bool LowStock, bool IsFractionable, bool IsActive);
