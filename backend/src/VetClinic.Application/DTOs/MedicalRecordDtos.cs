namespace VetClinic.Application.DTOs;

public record AddMedicalRecordRequest(int PetId, int? VetId, int? AppointmentId, string Symptoms, string Findings, string Diagnosis, string TreatmentPlan, decimal? WeightKg, decimal? TemperatureC);

public record AddPrescriptionRequest(string MedicationName, string Dosage, string Frequency, string Duration, string Instructions);

public record AddLabResultRequest(string TestType, string Result, string ReferenceRange, bool IsAbnormal);

public record PrescriptionDto(int Id, string MedicationName, string Dosage, string Frequency, string Duration, string Instructions);

public record LabResultDto(int Id, string TestType, string Result, string ReferenceRange, bool IsAbnormal, DateTime ResultDate);

public record AttachmentDto(int Id, string FileName, string FilePath, string ContentType, long SizeBytes, DateTime UploadedAt);

public record MedicalRecordDetailDto(
    int Id,
    DateTime RecordDate,
    string Symptoms,
    string Findings,
    string Diagnosis,
    string TreatmentPlan,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? PetId,
    int? VetId,
    string? VetName,
    IReadOnlyList<PrescriptionDto> Prescriptions,
    IReadOnlyList<LabResultDto> LabResults,
    IReadOnlyList<AttachmentDto> Attachments);