using VetClinic.Domain.Common;
using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Emitida;
    public decimal Subtotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = InvoiceCurrencies.Default;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int? TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public int? PetId { get; set; }
    public Pet? Pet { get; set; }
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}