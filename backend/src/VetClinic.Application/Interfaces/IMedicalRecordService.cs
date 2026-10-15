using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IMedicalRecordService
{
    Task<MedicalRecordDetailDto> AddAsync(AddMedicalRecordRequest r, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> UpdateAsync(int id, UpdateMedicalRecordRequest r, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<MedicalRecordDetailDto>> ListByPetAsync(int petId, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> GetDetailAsync(int id, CancellationToken ct = default);

    Task<MedicalRecordDetailDto?> AddPrescriptionAsync(int recordId, AddPrescriptionRequest r, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> UpdatePrescriptionAsync(int recordId, int prescriptionId, UpdatePrescriptionRequest r, CancellationToken ct = default);
    Task<bool> DeletePrescriptionAsync(int recordId, int prescriptionId, CancellationToken ct = default);

    Task<MedicalRecordDetailDto?> AddLabResultAsync(int recordId, AddLabResultRequest r, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> UpdateLabResultAsync(int recordId, int labResultId, UpdateLabResultRequest r, CancellationToken ct = default);
    Task<bool> DeleteLabResultAsync(int recordId, int labResultId, CancellationToken ct = default);

    Task<MedicalRecordDetailDto?> AddAttachmentFileAsync(int recordId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default);
    Task<bool> DeleteAttachmentAsync(int recordId, int attachmentId, CancellationToken ct = default);
    Task<AttachmentDownloadDto?> GetAttachmentAsync(int recordId, int attachmentId, CancellationToken ct = default);
}