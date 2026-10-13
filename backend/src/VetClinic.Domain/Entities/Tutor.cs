using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

public class Tutor : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}