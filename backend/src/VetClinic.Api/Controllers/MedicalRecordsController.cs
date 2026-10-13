using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;

namespace VetClinic.Api.Controllers;

[ApiController]
[Route("api/medical-records")]
[Authorize(Roles = "Veterinario,Administrador")]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _service;
    private readonly IFileStorage _fileStorage;

    public MedicalRecordsController(IMedicalRecordService service, IFileStorage fileStorage)
    {
        _service = service;
        _fileStorage = fileStorage;
    }

    [HttpPost]
    public async Task<IActionResult> Add(AddMedicalRecordRequest request, CancellationToken ct) =>
        Ok(await _service.AddAsync(request, ct));

    [HttpGet("pet/{petId:int}")]
    public async Task<IActionResult> ListByPet(int petId, CancellationToken ct) =>
        Ok(await _service.ListByPetAsync(petId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var record = await _service.GetDetailAsync(id, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{recordId:int}/prescriptions")]
    public async Task<IActionResult> AddPrescription(int recordId, AddPrescriptionRequest request, CancellationToken ct)
    {
        var record = await _service.AddPrescriptionAsync(recordId, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{recordId:int}/lab-results")]
    public async Task<IActionResult> AddLabResult(int recordId, AddLabResultRequest request, CancellationToken ct)
    {
        var record = await _service.AddLabResultAsync(recordId, request, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{recordId:int}/attachments")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> AddAttachment(int recordId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Se requiere un archivo.");

        await using var stream = file.OpenReadStream();
        var record = await _service.AddAttachmentFileAsync(recordId, file.FileName, file.ContentType, file.Length, stream, ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpGet("{recordId:int}/attachments/{attachmentId:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int recordId, int attachmentId, CancellationToken ct, bool download = false)
    {
        var attachment = await _service.GetAttachmentAsync(recordId, attachmentId, ct);
        if (attachment is null) return NotFound();

        var path = _fileStorage.GetPhysicalPath(attachment.FilePath);
        if (path is null) return NotFound();

        var inline = !download && attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return PhysicalFile(path, attachment.ContentType, enableRangeProcessing: true, fileDownloadName: inline ? null : attachment.FileName);
    }
}