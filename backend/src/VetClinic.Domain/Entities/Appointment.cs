using VetClinic.Domain.Common;
using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Entities;

public class Appointment : BaseEntity
{
    public DateTime StartTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Baja;
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Programada;
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? PetId { get; set; }
    public Pet? Pet { get; set; }
    public int? VetId { get; set; }
    public User? Vet { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
}