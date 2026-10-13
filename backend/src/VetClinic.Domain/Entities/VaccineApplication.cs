using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class VaccineApplication : BaseEntity
{
    public int PetId { get; set; }
    public Pet? Pet { get; set; }
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public DateTime? NextDueDate { get; set; }
}