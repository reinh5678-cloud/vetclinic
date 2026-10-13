using VetClinic.Domain.Common;
using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Entities;

public class Pet : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Species Species { get; set; }
    public string Breed { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public Sex Sex { get; set; }
    public decimal WeightKg { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool IsSterilized { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int? TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public ICollection<Hospitalization> Hospitalizations { get; set; } = new List<Hospitalization>();
    public ICollection<VaccineApplication> VaccineApplications { get; set; } = new List<VaccineApplication>();
}