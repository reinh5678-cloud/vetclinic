using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class LabResult : BaseEntity
{
    public string TestType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string ReferenceRange { get; set; } = string.Empty;
    public bool IsAbnormal { get; set; }
    public DateTime ResultDate { get; set; } = DateTime.UtcNow;
    public int MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}