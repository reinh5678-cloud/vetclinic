using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class MedicationAdministration : BaseEntity
{
    public string MedicationName { get; set; } = string.Empty;
    public string Dose { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public DateTime AdministeredAt { get; set; } = DateTime.UtcNow;
    public int HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }
    public int? InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public int? AdministeredById { get; set; }
    public User? AdministeredBy { get; set; }
}