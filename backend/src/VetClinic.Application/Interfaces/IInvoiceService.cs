using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InvoiceDto>> ListAsync(CancellationToken ct = default);
    Task<InvoiceDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<InvoiceDto?> UpdateStatusAsync(int id, UpdateInvoiceStatusRequest request, CancellationToken ct = default);
}