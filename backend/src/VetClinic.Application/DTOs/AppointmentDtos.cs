using VetClinic.Domain.Enums;

namespace VetClinic.Application.DTOs;

public record CreateAppointmentRequest(DateTime StartTime, int DurationMinutes, UrgencyLevel Urgency, string Reason, int? PetId, int? VetId);

public record UpdateAppointmentStatusRequest(AppointmentStatus Status, string Notes);

public record AppointmentDto(int Id, DateTime StartTime, int DurationMinutes, string Urgency, string Status, string Reason, string Notes, int? PetId, string? PetName, int? VetId, string? VetName);