using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class InvoiceLine : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total => Quantity * UnitPrice;
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
}