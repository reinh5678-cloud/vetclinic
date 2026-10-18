using VetClinic.Domain.Enums;

namespace VetClinic.Application.DTOs;

public record StartHospitalizationRequest(int PetId, int? AssignedVetId, string Reason, string Notes);

public record AddEvolutionRequest(decimal? Temperature, int? HeartRate, int? RespiratoryRate, string Observations);

public record AddMedicationAdministrationRequest(string MedicationName, string Dose, string Route, DateTime AdministeredAt, int? InventoryItemId = null, decimal Quantity = 1);

public record ChangeStatusRequest(string Status);

public record HospitalizationDto(int Id, DateTime StartDate, DateTime? EndDate, string Status, string Reason, int PetId, string? PetName, int? AssignedVetId, string? AssignedVetName);

public record EvolutionDto(int Id, DateTime RecordedAt, decimal? Temperature, int? HeartRate, int? RespiratoryRate, string Observations, string? RecordedByName);

public record MedicationDto(int Id, string MedicationName, string Dose, string Route, DateTime AdministeredAt, string? AdministeredByName, int? InventoryItemId);

public record HospitalizationDetailDto(
    int Id,
    DateTime StartDate,
    DateTime? EndDate,
    string Status,
    string Reason,
    string Notes,
    int PetId,
    string? PetName,
    int? AssignedVetId,
    string? AssignedVetName,
    IReadOnlyList<EvolutionDto> Evolutions,
    IReadOnlyList<MedicationDto> Medications);