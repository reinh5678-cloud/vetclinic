using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class HospitalizationEvolution : BaseEntity
{
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public decimal? Temperature { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public string Observations { get; set; } = string.Empty;
    public int HospitalizationId { get; set; }
    public Hospitalization? Hospitalization { get; set; }
    public int? RecordedById { get; set; }
    public User? RecordedBy { get; set; }
}