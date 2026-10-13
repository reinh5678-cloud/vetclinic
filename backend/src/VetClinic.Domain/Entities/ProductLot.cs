using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class ProductLot : BaseEntity
{
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal AvailableQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}