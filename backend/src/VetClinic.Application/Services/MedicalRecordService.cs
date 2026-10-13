using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IRepository<MedicalRecord> _records;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly IRepository<LabResult> _labResults;
    private readonly IRepository<Attachment> _attachments;
    private readonly IFileStorage _fileStorage;

    public MedicalRecordService(
        IRepository<MedicalRecord> records,
        IRepository<Prescription> prescriptions,
        IRepository<LabResult> labResults,
        IRepository<Attachment> attachments,
        IFileStorage fileStorage)
    {
        _records = records;
        _prescriptions = prescriptions;
        _labResults = labResults;
        _attachments = attachments;
        _fileStorage = fileStorage;
    }

    public async Task<MedicalRecordDetailDto> AddAsync(AddMedicalRecordRequest r, CancellationToken ct = default)
    {
        var record = new MedicalRecord
        {
            PetId = r.PetId,
            VetId = r.VetId,
            AppointmentId = r.AppointmentId,
            Symptoms = r.Symptoms,
            Findings = r.Findings,
            Diagnosis = r.Diagnosis,
            TreatmentPlan = r.TreatmentPlan,
            WeightKg = r.WeightKg,
            TemperatureC = r.TemperatureC
        };
        await _records.AddAsync(record, ct);
        await _records.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(record.Id, ct);
        if (loaded is null) throw new InvalidOperationException("Failed to load medical record after creation");
        return Map(loaded);
    }

    public async Task<IReadOnlyList<MedicalRecordDetailDto>> ListByPetAsync(int petId, CancellationToken ct = default)
    {
        var list = await _records.Query()
            .Where(r => r.PetId == petId)
            .OrderByDescending(r => r.RecordDate)
            .Include(r => r.Vet)
            .Include(r => r.Prescriptions)
            .Include(r => r.LabResults)
            .Include(r => r.Attachments)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<MedicalRecordDetailDto?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        var record = await LoadDetailAsync(id, ct);
        return record is null ? null : Map(record);
    }

    public async Task<MedicalRecordDetailDto?> AddPrescriptionAsync(int recordId, AddPrescriptionRequest r, CancellationToken ct = default)
    {
        var record = await _records.GetByIdAsync(recordId, ct);
        if (record is null) return null;
        await _prescriptions.AddAsync(new Prescription
        {
            MedicalRecordId = recordId,
            MedicationName = r.MedicationName,
            Dosage = r.Dosage,
            Frequency = r.Frequency,
            Duration = r.Duration,
            Instructions = r.Instructions
        }, ct);
        await _prescriptions.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(recordId, ct);
        return loaded is null ? null : Map(loaded);
    }

    public async Task<MedicalRecordDetailDto?> AddLabResultAsync(int recordId, AddLabResultRequest r, CancellationToken ct = default)
    {
        var record = await _records.GetByIdAsync(recordId, ct);
        if (record is null) return null;
        await _labResults.AddAsync(new LabResult
        {
            MedicalRecordId = recordId,
            TestType = r.TestType,
            Result = r.Result,
            ReferenceRange = r.ReferenceRange,
            IsAbnormal = r.IsAbnormal
        }, ct);
        await _labResults.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(recordId, ct);
        return loaded is null ? null : Map(loaded);
    }

    public async Task<MedicalRecordDetailDto?> AddAttachmentFileAsync(int recordId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default)
    {
        var record = await _records.GetByIdAsync(recordId, ct);
        if (record is null) return null;

        var relativePath = await _fileStorage.SaveAsync(recordId.ToString(), fileName, content, ct);
        await _attachments.AddAsync(new Attachment
        {
            MedicalRecordId = recordId,
            FileName = Path.GetFileName(fileName),
            FilePath = relativePath,
            ContentType = contentType,
            SizeBytes = sizeBytes
        }, ct);
        await _attachments.SaveChangesAsync(ct);
        var loaded = await LoadDetailAsync(recordId, ct);
        return loaded is null ? null : Map(loaded);
    }

    public async Task<AttachmentDto?> GetAttachmentAsync(int recordId, int attachmentId, CancellationToken ct = default)
    {
        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.MedicalRecordId == recordId, ct);
        return attachment is null ? null
            : new AttachmentDto(attachment.Id, attachment.FileName, attachment.FilePath, attachment.ContentType, attachment.SizeBytes, attachment.UploadedAt);
    }

    private async Task<MedicalRecord?> LoadDetailAsync(int id, CancellationToken ct) =>
        await _records.Query()
            .Include(r => r.Vet)
            .Include(r => r.Prescriptions)
            .Include(r => r.LabResults)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    private static MedicalRecordDetailDto Map(MedicalRecord r)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(r.Prescriptions);
        ArgumentNullException.ThrowIfNull(r.LabResults);
        ArgumentNullException.ThrowIfNull(r.Attachments);
        return new(
            r.Id,
            r.RecordDate,
            r.Symptoms,
            r.Findings,
            r.Diagnosis,
            r.TreatmentPlan,
            r.WeightKg,
            r.TemperatureC,
            r.PetId,
            r.VetId,
            r.Vet?.FullName,
            r.Prescriptions.Select(p => new PrescriptionDto(p.Id, p.MedicationName, p.Dosage, p.Frequency, p.Duration, p.Instructions)).ToList(),
            r.LabResults.Select(l => new LabResultDto(l.Id, l.TestType, l.Result, l.ReferenceRange, l.IsAbnormal, l.ResultDate)).ToList(),
            r.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.FilePath, a.ContentType, a.SizeBytes, a.UploadedAt)).ToList());
    }
}