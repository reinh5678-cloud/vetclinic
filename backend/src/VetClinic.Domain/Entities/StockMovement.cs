using VetClinic.Domain.Common;
using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Entities;

public class StockMovement : BaseEntity
{
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public int? ProductLotId { get; set; }
    public ProductLot? ProductLot { get; set; }
    public int? PerformedById { get; set; }
    public User? PerformedBy { get; set; }
}