using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Api.Authorization;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/medical-records")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _service;

    public MedicalRecordsController(IMedicalRecordService service)
    {
        _service = service;
    }

    [HttpPost]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> Add(AddMedicalRecordRequest request, CancellationToken ct) =>
        Ok(await _service.AddAsync(request, ct));

    [HttpPut("{id:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> Update(int id, UpdateMedicalRecordRequest request, CancellationToken ct)
    {
        var record = await _service.UpdateAsync(id, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await _service.DeleteAsync(id, ct) ? Ok() : NotFound();

    [HttpGet("pet/{petId:int}")]
    [RequirePermission(Permissions.MedicalView)]
    public async Task<IActionResult> ListByPet(int petId, CancellationToken ct) =>
        Ok(await _service.ListByPetAsync(petId, ct));

    [HttpGet("{id:int}")]
    [RequirePermission(Permissions.MedicalView)]
    public async Task<IActionResult> GetDetail(int id, CancellationToken ct)
    {
        var record = await _service.GetDetailAsync(id, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{recordId:int}/prescriptions")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> AddPrescription(int recordId, AddPrescriptionRequest request, CancellationToken ct)
    {
        var record = await _service.AddPrescriptionAsync(recordId, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPut("{recordId:int}/prescriptions/{prescriptionId:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> UpdatePrescription(int recordId, int prescriptionId, UpdatePrescriptionRequest request, CancellationToken ct)
    {
        var record = await _service.UpdatePrescriptionAsync(recordId, prescriptionId, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpDelete("{recordId:int}/prescriptions/{prescriptionId:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> DeletePrescription(int recordId, int prescriptionId, CancellationToken ct) =>
        await _service.DeletePrescriptionAsync(recordId, prescriptionId, ct) ? Ok() : NotFound();

    [HttpPost("{recordId:int}/lab-results")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> AddLabResult(int recordId, AddLabResultRequest request, CancellationToken ct)
    {
        var record = await _service.AddLabResultAsync(recordId, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPut("{recordId:int}/lab-results/{labResultId:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> UpdateLabResult(int recordId, int labResultId, UpdateLabResultRequest request, CancellationToken ct)
    {
        var record = await _service.UpdateLabResultAsync(recordId, labResultId, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpDelete("{recordId:int}/lab-results/{labResultId:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> DeleteLabResult(int recordId, int labResultId, CancellationToken ct) =>
        await _service.DeleteLabResultAsync(recordId, labResultId, ct) ? Ok() : NotFound();

    [HttpPost("{recordId:int}/attachments")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> AddAttachment(int recordId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Se requiere un archivo.");

        await using var stream = file.OpenReadStream();
        var record = await _service.AddAttachmentFileAsync(recordId, file.FileName, file.ContentType, file.Length, stream, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpDelete("{recordId:int}/attachments/{attachmentId:int}")]
    [RequirePermission(Permissions.MedicalManage)]
    public async Task<IActionResult> DeleteAttachment(int recordId, int attachmentId, CancellationToken ct) =>
        await _service.DeleteAttachmentAsync(recordId, attachmentId, ct) ? Ok() : NotFound();

    [HttpGet("{recordId:int}/attachments/{attachmentId:int}/download")]
    [RequirePermission(Permissions.MedicalView)]
    public async Task<IActionResult> DownloadAttachment(int recordId, int attachmentId, CancellationToken ct, bool download = false)
    {
        var attachment = await _service.GetAttachmentAsync(recordId, attachmentId, ct);
        if (attachment is null) return NotFound();

        var inline = !download && attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return PhysicalFile(attachment.PhysicalPath, attachment.ContentType, enableRangeProcessing: true, fileDownloadName: inline ? null : attachment.FileName);
    }
}