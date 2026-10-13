using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    public string Symptoms { get; set; } = string.Empty;
    public string Findings { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatmentPlan { get; set; } = string.Empty;
    public decimal? WeightKg { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? PetId { get; set; }
    public Pet? Pet { get; set; }
    public int? VetId { get; set; }
    public User? Vet { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}