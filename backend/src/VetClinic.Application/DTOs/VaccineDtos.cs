namespace VetClinic.Application.DTOs;

public record AddVaccineRequest(int InventoryItemId, DateTime ApplicationDate, DateTime? NextDueDate);

public record VaccineApplicationDto(
    int Id,
    int PetId,
    int? InventoryItemId,
    string? VaccineName,
    DateTime ApplicationDate,
    DateTime? NextDueDate,
    bool IsDueSoon);

public record UpcomingVaccineDto(
    int PetId,
    string PetName,
    string? TutorName,
    string? VaccineName,
    DateTime ApplicationDate,
    DateTime NextDueDate,
    bool IsOverdue);
