using VetClinic.Domain.Common;
using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Entities;

public class Hospitalization : BaseEntity
{
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public HospitalizationStatus Status { get; set; } = HospitalizationStatus.Ingresado;
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int PetId { get; set; }
    public Pet? Pet { get; set; }
    public int? AssignedVetId { get; set; }
    public User? AssignedVet { get; set; }
    public ICollection<HospitalizationEvolution> Evolutions { get; set; } = new List<HospitalizationEvolution>();
    public ICollection<MedicationAdministration> MedicationAdministrations { get; set; } = new List<MedicationAdministration>();
}