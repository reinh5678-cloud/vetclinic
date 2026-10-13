using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class Prescription : BaseEntity
{
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public int MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}