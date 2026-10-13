using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class Attachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int? MedicalRecordId { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}