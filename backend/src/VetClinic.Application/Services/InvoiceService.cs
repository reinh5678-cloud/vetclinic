using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class InvoiceService : IInvoiceService
{
    private static readonly Dictionary<InvoiceStatus, InvoiceStatus[]> AllowedTransitions = new()
    {
        [InvoiceStatus.Borrador] = new[] { InvoiceStatus.Emitida, InvoiceStatus.Anulada },
        [InvoiceStatus.Emitida] = new[] { InvoiceStatus.Pagada, InvoiceStatus.Anulada },
        [InvoiceStatus.Pagada] = Array.Empty<InvoiceStatus>(),
        [InvoiceStatus.Anulada] = Array.Empty<InvoiceStatus>()
    };

    private readonly IRepository<Invoice> _invoices;
    private readonly IRepository<InventoryItem> _inventory;
    private readonly IRepository<ProductLot> _lots;
    private readonly IRepository<StockMovement> _movements;
    private readonly IRepository<Tutor> _tutors;
    private readonly IRepository<Pet> _pets;
    private readonly IUnitOfWork _uow;
    private readonly ISettingsService _settings;
    private readonly ICurrentUser _currentUser;

    public InvoiceService(
        IRepository<Invoice> invoices,
        IRepository<InventoryItem> inventory,
        IRepository<ProductLot> lots,
        IRepository<StockMovement> movements,
        IRepository<Tutor> tutors,
        IRepository<Pet> pets,
        IUnitOfWork uow,
        ISettingsService settings,
        ICurrentUser currentUser)
    {
        _invoices = invoices;
        _inventory = inventory;
        _lots = lots;
        _movements = movements;
        _tutors = tutors;
        _pets = pets;
        _uow = uow;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest r, CancellationToken ct = default)
    {
        var billing = await _settings.GetBillingAsync(ct);

        if (r.Lines is null || r.Lines.Count == 0)
            throw new ArgumentException("La factura debe tener al menos una línea");
        if (!Enum.IsDefined(r.Status))
            throw new ArgumentException("Estado de factura inválido");
        if (!InvoiceCurrencies.IsValid(r.Currency))
            throw new ArgumentException($"Moneda inválida. Valores permitidos: {string.Join(", ", InvoiceCurrencies.All)}");
        if (string.IsNullOrWhiteSpace(r.PaymentMethod) ||
            !billing.PaymentMethods.Contains(r.PaymentMethod, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Medio de pago inválido. Valores permitidos: {string.Join(", ", billing.PaymentMethods)}");
        if (!r.TutorId.HasValue && !r.PetId.HasValue)
            throw new ArgumentException("La factura debe indicar el tutor o la mascota del cliente");
        if (r.TutorId.HasValue && await _tutors.GetByIdAsync(r.TutorId.Value, ct) is null)
            throw new ArgumentException($"El tutor {r.TutorId.Value} no existe");
        if (r.PetId.HasValue && await _pets.GetByIdAsync(r.PetId.Value, ct) is null)
            throw new ArgumentException($"La mascota {r.PetId.Value} no existe");

        var invoice = new Invoice
        {
            TutorId = r.TutorId,
            PetId = r.PetId,
            Status = r.Status,
            PaymentMethod = r.PaymentMethod,
            Currency = r.Currency.Trim().ToUpperInvariant(),
            InvoiceNumber = GenerateNumber(billing.InvoicePrefix),
            CreatedById = _currentUser.UserId
        };

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            foreach (var line in r.Lines)
            {
                if (line.Quantity <= 0)
                    throw new ArgumentException("La cantidad de cada línea debe ser mayor a cero");

                var invoiceLine = new InvoiceLine
                {
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    InventoryItemId = line.InventoryItemId
                };

                if (line.InventoryItemId.HasValue)
                    await ConsumeFefoAsync(line.InventoryItemId.Value, line.Quantity, invoice.InvoiceNumber, ct);

                invoice.Lines.Add(invoiceLine);
            }

            invoice.Subtotal = invoice.Lines.Sum(l => l.Total);
            invoice.Taxes = Math.Round(invoice.Subtotal * billing.TaxRate, 2);
            invoice.Total = invoice.Subtotal + invoice.Taxes;

            await _invoices.AddAsync(invoice, ct);
            await _invoices.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        return Map(invoice);
    }

    private async Task ConsumeFefoAsync(int itemId, decimal quantity, string invoiceNumber, CancellationToken ct)
    {
        var item = await _inventory.GetByIdAsync(itemId, ct);
        if (item is null || !item.IsActive)
            throw new InvalidOperationException("Artículo de inventario no encontrado o inactivo");

        if (!item.IsFractionable && quantity != Math.Truncate(quantity))
            throw new ArgumentException($"«{item.Name}» no admite cantidades fraccionadas (unidad entera)");

        await Fefo.ConsumeAsync(item, _lots, _movements, quantity, StockMovementType.SalidaVenta,
            $"Venta factura {invoiceNumber}", _currentUser.UserId, ct);

        _inventory.Update(item);
    }

    public async Task<IReadOnlyList<InvoiceDto>> ListAsync(CancellationToken ct = default)
    {
        var list = await _invoices.Query()
            .OrderByDescending(i => i.IssueDate)
            .Include(i => i.Tutor)
            .Take(300)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<InvoiceDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var invoice = await _invoices.Query()
            .Include(i => i.Tutor)
            .Include(i => i.Pet)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return invoice is null ? null : Map(invoice);
    }

    public async Task<InvoiceDto?> UpdateStatusAsync(int id, UpdateInvoiceStatusRequest r, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(r.Status))
            throw new ArgumentException("Estado de factura inválido");

        var invoice = await _invoices.GetByIdAsync(id, ct);
        if (invoice is null) return null;

        if (invoice.Status != r.Status &&
            !AllowedTransitions.GetValueOrDefault(invoice.Status, Array.Empty<InvoiceStatus>()).Contains(r.Status))
            throw new InvalidOperationException($"No se puede pasar la factura de «{invoice.Status}» a «{r.Status}»");

        invoice.Status = r.Status;
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync(ct);

        var loaded = await _invoices.Query()
            .Include(i => i.Tutor)
            .Include(i => i.Pet)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return loaded is null ? null : Map(loaded);
    }

    private static string GenerateNumber(string prefix)
    {
        var clean = string.IsNullOrWhiteSpace(prefix) ? "FAC" : prefix.Trim();
        return $"{clean}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static InvoiceLineDto Map(InvoiceLine l) => new(
        l.Id, l.Description, l.Quantity, l.UnitPrice, l.Total, l.InventoryItemId);

    private static InvoiceDto Map(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.IssueDate, i.Status.ToString(), i.Subtotal, i.Taxes, i.Total,
        string.IsNullOrWhiteSpace(i.Currency) ? InvoiceCurrencies.Default : i.Currency,
        i.PaymentMethod, i.TutorId, i.Tutor is null ? null : $"{i.Tutor.FirstName} {i.Tutor.LastName}",
        i.PetId, i.Pet?.Name, i.Notes,
        (i.Lines ?? new List<InvoiceLine>()).Select(Map).ToList());
}