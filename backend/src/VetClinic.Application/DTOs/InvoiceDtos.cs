using VetClinic.Domain.Enums;

namespace VetClinic.Application.DTOs;

public record InvoiceLineRequest(string Description, decimal Quantity, decimal UnitPrice, int? InventoryItemId);

public record CreateInvoiceRequest(int? TutorId, int? PetId, InvoiceStatus Status, string PaymentMethod, IReadOnlyList<InvoiceLineRequest> Lines);

public record UpdateInvoiceStatusRequest(InvoiceStatus Status);

public record InvoiceLineDto(int Id, string Description, decimal Quantity, decimal UnitPrice, decimal Total, int? InventoryItemId);

public record InvoiceDto(int Id, string InvoiceNumber, DateTime IssueDate, string Status, decimal Subtotal, decimal Taxes, decimal Total, string PaymentMethod, int? TutorId, string? TutorName, int? PetId, string? PetName, string Notes, IReadOnlyList<InvoiceLineDto> Lines);