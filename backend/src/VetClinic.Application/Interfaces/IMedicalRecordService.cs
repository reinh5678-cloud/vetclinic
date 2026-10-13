using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IMedicalRecordService
{
    Task<MedicalRecordDetailDto> AddAsync(AddMedicalRecordRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MedicalRecordDetailDto>> ListByPetAsync(int petId, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> GetDetailAsync(int id, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> AddPrescriptionAsync(int recordId, AddPrescriptionRequest request, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> AddLabResultAsync(int recordId, AddLabResultRequest request, CancellationToken ct = default);
    Task<MedicalRecordDetailDto?> AddAttachmentFileAsync(int recordId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default);
    Task<AttachmentDto?> GetAttachmentAsync(int recordId, int attachmentId, CancellationToken ct = default);
}