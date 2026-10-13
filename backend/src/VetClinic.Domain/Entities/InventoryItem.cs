using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal StockQuantity { get; set; }
    public decimal MinStock { get; set; }
    public decimal UnitPrice { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public bool IsFractionable { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
    public ICollection<ProductLot> Lots { get; set; } = new List<ProductLot>();
    public ICollection<VaccineApplication> VaccineApplications { get; set; } = new List<VaccineApplication>();
}