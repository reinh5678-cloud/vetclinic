using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class Reminder : BaseEntity
{
    public string Recipient { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsSent { get; set; }
    public int? PetId { get; set; }
    public Pet? Pet { get; set; }
}