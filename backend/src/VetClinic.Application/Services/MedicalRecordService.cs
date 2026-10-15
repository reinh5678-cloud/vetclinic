using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private const string VetRoleName = "Veterinario";
    private const int MaxListSize = 200;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tif", ".tiff",
        ".pdf", ".txt", ".csv", ".doc", ".docx", ".xls", ".xlsx", ".dcm"
    };

    private readonly IRepository<MedicalRecord> _records;
    private readonly IRepository<Prescription> _prescriptions;
    private readonly IRepository<LabResult> _labResults;
    private readonly IRepository<Attachment> _attachments;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<User> _users;
    private readonly IRepository<Appointment> _appointments;
    private readonly IFileStorage _fileStorage;

    public MedicalRecordService(
        IRepository<MedicalRecord> records,
        IRepository<Prescription> prescriptions,
        IRepository<LabResult> labResults,
        IRepository<Attachment> attachments,
        IRepository<Pet> pets,
        IRepository<User> users,
        IRepository<Appointment> appointments,
        IFileStorage fileStorage)
    {
        _records = records;
        _prescriptions = prescriptions;
        _labResults = labResults;
        _attachments = attachments;
        _pets = pets;
        _users = users;
        _appointments = appointments;
        _fileStorage = fileStorage;
    }

    public async Task<MedicalRecordDetailDto> AddAsync(AddMedicalRecordRequest r, CancellationToken ct = default)
    {
        await EnsurePetAsync(r.PetId, ct);
        await EnsureVetAsync(r.VetId, ct);
        await EnsureAppointmentAsync(r.AppointmentId, r.PetId, null, ct);
        ValidateVitals(r.WeightKg, r.TemperatureC);

        var record = new MedicalRecord
        {
            PetId = r.PetId,
            VetId = r.VetId,
            AppointmentId = r.AppointmentId,
            Symptoms = Required(r.Symptoms, "Síntomas", 2000),
            Findings = Optional(r.Findings, "Hallazgos", 2000),
            Diagnosis = Optional(r.Diagnosis, "Diagnóstico", 1000),
            TreatmentPlan = Optional(r.TreatmentPlan, "Plan de tratamiento", 2000),
            WeightKg = r.WeightKg,
            TemperatureC = r.TemperatureC
        };
        await _records.AddAsync(record, ct);
        await _records.SaveChangesAsync(ct);
        return await ReloadAsync(record.Id, ct);
    }

    public async Task<MedicalRecordDetailDto?> UpdateAsync(int id, UpdateMedicalRecordRequest r, CancellationToken ct = default)
    {
        var record = await _records.GetByIdAsync(id, ct);
        if (record is null) return null;

        await EnsureVetAsync(r.VetId, ct);
        ValidateVitals(r.WeightKg, r.TemperatureC);

        record.Symptoms = Required(r.Symptoms, "Síntomas", 2000);
        record.Findings = Optional(r.Findings, "Hallazgos", 2000);
        record.Diagnosis = Optional(r.Diagnosis, "Diagnóstico", 1000);
        record.TreatmentPlan = Optional(r.TreatmentPlan, "Plan de tratamiento", 2000);
        record.WeightKg = r.WeightKg;
        record.TemperatureC = r.TemperatureC;
        if (r.VetId.HasValue) record.VetId = r.VetId;
        if (r.RecordDate.HasValue) record.RecordDate = r.RecordDate.Value;

        _records.Update(record);
        await _records.SaveChangesAsync(ct);
        return await ReloadAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var record = await _records.Query()
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return false;

        foreach (var attachment in record.Attachments)
            _fileStorage.Delete(attachment.FilePath);

        _records.Remove(record);
        await _records.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<MedicalRecordDetailDto>> ListByPetAsync(int petId, CancellationToken ct = default)
    {
        var list = await _records.Query()
            .AsNoTracking()
            .Where(r => r.PetId == petId)
            .OrderByDescending(r => r.RecordDate)
            .Include(r => r.Vet)
            .Include(r => r.Prescriptions)
            .Include(r => r.LabResults)
            .Include(r => r.Attachments)
            .AsSplitQuery()
            .Take(MaxListSize)
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
        if (!await _records.Query().AnyAsync(x => x.Id == recordId, ct)) return null;
        await _prescriptions.AddAsync(new Prescription
        {
            MedicalRecordId = recordId,
            MedicationName = Required(r.MedicationName, "Medicamento", 120),
            Dosage = Optional(r.Dosage, "Dosis", 2000),
            Frequency = Optional(r.Frequency, "Frecuencia", 2000),
            Duration = Optional(r.Duration, "Duración", 2000),
            Instructions = Optional(r.Instructions, "Instrucciones", 2000)
        }, ct);
        await _prescriptions.SaveChangesAsync(ct);
        return await ReloadAsync(recordId, ct);
    }

    public async Task<MedicalRecordDetailDto?> UpdatePrescriptionAsync(int recordId, int prescriptionId, UpdatePrescriptionRequest r, CancellationToken ct = default)
    {
        var prescription = await _prescriptions.Query()
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.MedicalRecordId == recordId, ct);
        if (prescription is null) return null;

        prescription.MedicationName = Required(r.MedicationName, "Medicamento", 120);
        prescription.Dosage = Optional(r.Dosage, "Dosis", 2000);
        prescription.Frequency = Optional(r.Frequency, "Frecuencia", 2000);
        prescription.Duration = Optional(r.Duration, "Duración", 2000);
        prescription.Instructions = Optional(r.Instructions, "Instrucciones", 2000);
        _prescriptions.Update(prescription);
        await _prescriptions.SaveChangesAsync(ct);
        return await ReloadAsync(recordId, ct);
    }

    public async Task<bool> DeletePrescriptionAsync(int recordId, int prescriptionId, CancellationToken ct = default)
    {
        var prescription = await _prescriptions.Query()
            .FirstOrDefaultAsync(p => p.Id == prescriptionId && p.MedicalRecordId == recordId, ct);
        if (prescription is null) return false;
        _prescriptions.Remove(prescription);
        await _prescriptions.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MedicalRecordDetailDto?> AddLabResultAsync(int recordId, AddLabResultRequest r, CancellationToken ct = default)
    {
        if (!await _records.Query().AnyAsync(x => x.Id == recordId, ct)) return null;
        await _labResults.AddAsync(new LabResult
        {
            MedicalRecordId = recordId,
            TestType = Required(r.TestType, "Tipo de prueba", 100),
            Result = Optional(r.Result, "Resultado", 1000),
            ReferenceRange = Optional(r.ReferenceRange, "Rango de referencia", 2000),
            IsAbnormal = r.IsAbnormal
        }, ct);
        await _labResults.SaveChangesAsync(ct);
        return await ReloadAsync(recordId, ct);
    }

    public async Task<MedicalRecordDetailDto?> UpdateLabResultAsync(int recordId, int labResultId, UpdateLabResultRequest r, CancellationToken ct = default)
    {
        var labResult = await _labResults.Query()
            .FirstOrDefaultAsync(l => l.Id == labResultId && l.MedicalRecordId == recordId, ct);
        if (labResult is null) return null;

        labResult.TestType = Required(r.TestType, "Tipo de prueba", 100);
        labResult.Result = Optional(r.Result, "Resultado", 1000);
        labResult.ReferenceRange = Optional(r.ReferenceRange, "Rango de referencia", 2000);
        labResult.IsAbnormal = r.IsAbnormal;
        _labResults.Update(labResult);
        await _labResults.SaveChangesAsync(ct);
        return await ReloadAsync(recordId, ct);
    }

    public async Task<bool> DeleteLabResultAsync(int recordId, int labResultId, CancellationToken ct = default)
    {
        var labResult = await _labResults.Query()
            .FirstOrDefaultAsync(l => l.Id == labResultId && l.MedicalRecordId == recordId, ct);
        if (labResult is null) return false;
        _labResults.Remove(labResult);
        await _labResults.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MedicalRecordDetailDto?> AddAttachmentFileAsync(int recordId, string fileName, string contentType, long sizeBytes, Stream content, CancellationToken ct = default)
    {
        if (!await _records.Query().AnyAsync(x => x.Id == recordId, ct)) return null;

        var safeName = Path.GetFileName(fileName)?.Trim();
        if (string.IsNullOrWhiteSpace(safeName))
            throw new ArgumentException("El nombre del archivo no es válido");
        if (!AllowedExtensions.Contains(Path.GetExtension(safeName)))
            throw new ArgumentException("Tipo de archivo no permitido. Se aceptan imágenes, PDF y documentos de oficina.");
        if (sizeBytes <= 0)
            throw new ArgumentException("El archivo está vacío");

        var relativePath = await _fileStorage.SaveAsync(recordId.ToString(), safeName, content, ct);
        await _attachments.AddAsync(new Attachment
        {
            MedicalRecordId = recordId,
            FileName = safeName,
            FilePath = relativePath,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = sizeBytes
        }, ct);
        await _attachments.SaveChangesAsync(ct);
        return await ReloadAsync(recordId, ct);
    }

    public async Task<bool> DeleteAttachmentAsync(int recordId, int attachmentId, CancellationToken ct = default)
    {
        var attachment = await _attachments.Query()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.MedicalRecordId == recordId, ct);
        if (attachment is null) return false;

        _attachments.Remove(attachment);
        await _attachments.SaveChangesAsync(ct);
        _fileStorage.Delete(attachment.FilePath);
        return true;
    }

    public async Task<AttachmentDownloadDto?> GetAttachmentAsync(int recordId, int attachmentId, CancellationToken ct = default)
    {
        var attachment = await _attachments.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.MedicalRecordId == recordId, ct);
        if (attachment is null) return null;

        var path = _fileStorage.GetPhysicalPath(attachment.FilePath);
        if (path is null) return null;

        return new AttachmentDownloadDto(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, path);
    }

    private async Task<MedicalRecordDetailDto> ReloadAsync(int id, CancellationToken ct)
    {
        var loaded = await LoadDetailAsync(id, ct);
        if (loaded is null) throw new KeyNotFoundException("La consulta indicada ya no existe");
        return Map(loaded);
    }

    private async Task<MedicalRecord?> LoadDetailAsync(int id, CancellationToken ct) =>
        await _records.Query()
            .AsSplitQuery()
            .Include(r => r.Vet)
            .Include(r => r.Prescriptions)
            .Include(r => r.LabResults)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    private async Task EnsurePetAsync(int petId, CancellationToken ct)
    {
        if (!await _pets.Query().AnyAsync(p => p.Id == petId, ct))
            throw new ArgumentException("El paciente indicado no existe");
    }

    private async Task EnsureVetAsync(int? vetId, CancellationToken ct)
    {
        if (vetId is null) return;
        var vet = await _users.Query().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == vetId, ct);
        if (vet is null)
            throw new ArgumentException("El veterinario indicado no existe");
        if (!vet.IsActive || vet.Role?.Name != VetRoleName)
            throw new ArgumentException("El usuario indicado no es un veterinario activo");
    }

    private async Task EnsureAppointmentAsync(int? appointmentId, int petId, int? excludeRecordId, CancellationToken ct)
    {
        if (appointmentId is null) return;
        var appointment = await _appointments.GetByIdAsync(appointmentId.Value, ct);
        if (appointment is null)
            throw new ArgumentException("La cita indicada no existe");
        if (appointment.PetId != petId)
            throw new ArgumentException("La cita no corresponde al paciente indicado");
        var alreadyLinked = await _records.Query()
            .AnyAsync(r => r.AppointmentId == appointmentId && r.Id != (excludeRecordId ?? 0), ct);
        if (alreadyLinked)
            throw new InvalidOperationException("La cita ya tiene una consulta asociada");
    }

    private static void ValidateVitals(decimal? weightKg, decimal? temperatureC)
    {
        if (weightKg is < 0 or > 300m)
            throw new ArgumentException("El peso debe estar entre 0 y 300 kg");
        if (temperatureC is < 0 or > 60m)
            throw new ArgumentException("La temperatura debe estar entre 0 y 60 °C");
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException($"El campo «{field}» es obligatorio");
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"El campo «{field}» supera el máximo de {maxLength} caracteres");
        return trimmed;
    }

    private static string Optional(string? value, string field, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"El campo «{field}» supera el máximo de {maxLength} caracteres");
        return trimmed;
    }

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
            r.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAt)).ToList());
    }
}