using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class VaccineService : IVaccineService
{
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<VaccineApplication> _applications;
    private readonly IRepository<InventoryItem> _inventory;
    private readonly IRepository<Reminder> _reminders;
    private readonly IRepository<ProductLot> _lots;
    private readonly IRepository<StockMovement> _movements;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;

    public VaccineService(
        IRepository<Pet> pets,
        IRepository<VaccineApplication> applications,
        IRepository<InventoryItem> inventory,
        IRepository<Reminder> reminders,
        IRepository<ProductLot> lots,
        IRepository<StockMovement> movements,
        IUnitOfWork uow,
        ICurrentUser currentUser)
    {
        _pets = pets;
        _applications = applications;
        _inventory = inventory;
        _reminders = reminders;
        _lots = lots;
        _movements = movements;
        _uow = uow;
        _currentUser = currentUser;
    }

public async Task<VaccineApplicationDto?> AddAsync(int petId, AddVaccineRequest r, CancellationToken ct = default)
    {
        var pet = await _pets.Query().Include(p => p.Tutor).FirstOrDefaultAsync(p => p.Id == petId, ct);
        if (pet is null) return null;

        var vaccine = await _inventory.GetByIdAsync(r.InventoryItemId, ct);
        if (vaccine is null)
            throw new ArgumentException("La vacuna seleccionada ya no existe en el inventario");
        if (!vaccine.Category.Equals("Vacunas", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"«{vaccine.Name}» no está clasificado como vacuna (categoría «{vaccine.Category}»). Solo se pueden aplicar ítems de la categoría Vacunas.");

        await using var tx = await _uow.BeginTransactionAsync(ct);
        try
        {
            await ConsumeDoseAsync(vaccine, ct);

            var application = new VaccineApplication
            {
                PetId = petId,
                InventoryItemId = r.InventoryItemId,
                ApplicationDate = r.ApplicationDate,
                NextDueDate = r.NextDueDate
            };
            await _applications.AddAsync(application, ct);

            if (r.NextDueDate.HasValue)
            {
                await _reminders.AddAsync(new Reminder
                {
                    PetId = petId,
                    Recipient = pet.Tutor?.Email ?? string.Empty,
                    Channel = "Correo",
                    Message = $"Vacuna {vaccine.Name} aplicada — próxima dosis {r.NextDueDate.Value:dd/MM/yyyy}",
                    ScheduledAt = r.NextDueDate.Value,
                    IsSent = false
                }, ct);
            }

            await _applications.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new VaccineApplicationDto(application.Id, petId, r.InventoryItemId, vaccine.Name, r.ApplicationDate, r.NextDueDate, IsDueSoon(r.NextDueDate));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task ConsumeDoseAsync(InventoryItem vaccine, CancellationToken ct)
    {
        if (!vaccine.IsActive)
            throw new InvalidOperationException($"«{vaccine.Name}» está inactivo; no se puede aplicar");

        const decimal dose = 1m;
        if (vaccine.StockQuantity < dose)
            throw new InvalidOperationException($"Stock insuficiente para «{vaccine.Name}» (disponible: {vaccine.StockQuantity:F3}, requerida: {dose:F3})");

        var lots = await _lots.Query()
            .Where(l => l.InventoryItemId == vaccine.Id && l.IsActive && l.AvailableQuantity > 0)
            .ToListAsync(ct);
        lots = lots
            .OrderByDescending(l => l.ExpirationDate.HasValue)
            .ThenBy(l => l.ExpirationDate)
            .ThenBy(l => l.Id)
            .ToList();

        var remaining = dose;
        foreach (var lot in lots)
        {
            if (remaining <= 0) break;

            var take = Math.Min(lot.AvailableQuantity, remaining);
            lot.AvailableQuantity -= take;
            remaining -= take;
            _lots.Update(lot);
            await _movements.AddAsync(new StockMovement
            {
                InventoryItemId = vaccine.Id,
                ProductLotId = lot.Id,
                Type = StockMovementType.SalidaUsoInterno,
                Quantity = take,
                Description = "Aplicación de vacuna",
                PerformedById = _currentUser.UserId
            }, ct);
        }

        if (remaining > 0)
            throw new InvalidOperationException($"Stock insuficiente para «{vaccine.Name}» (faltan {remaining:F3} disponibles en lotes activos)");

        vaccine.StockQuantity -= dose;
        _inventory.Update(vaccine);
    }

    public async Task<IReadOnlyList<VaccineApplicationDto>> ListByPetAsync(int petId, CancellationToken ct = default)
    {
        var list = await _applications.Query()
            .Include(a => a.InventoryItem)
            .Where(a => a.PetId == petId)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync(ct);

        return list.Select(a => new VaccineApplicationDto(
            a.Id,
            petId,
            a.InventoryItemId,
            a.InventoryItem?.Name,
            a.ApplicationDate,
            a.NextDueDate,
            IsDueSoon(a.NextDueDate))).ToList();
    }

    private static bool IsDueSoon(DateTime? nextDueDate) =>
        nextDueDate.HasValue && nextDueDate.Value.Date <= DateTime.UtcNow.AddDays(30);

    public async Task<IReadOnlyList<UpcomingVaccineDto>> ListUpcomingAsync(int days = 30, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var limit = today.AddDays(days);

        var list = await _applications.Query()
            .Include(a => a.InventoryItem)
            .Include(a => a.Pet).ThenInclude(p => p!.Tutor)
            .Where(a => a.NextDueDate != null && a.NextDueDate.Value.Date <= limit)
            .OrderBy(a => a.NextDueDate)
            .ToListAsync(ct);

        return list.Select(a => new UpcomingVaccineDto(
            a.PetId,
            a.Pet?.Name ?? "—",
            a.Pet?.Tutor is null ? null : $"{a.Pet.Tutor.FirstName} {a.Pet.Tutor.LastName}",
            a.InventoryItem?.Name,
            a.ApplicationDate,
            a.NextDueDate!.Value,
            a.NextDueDate!.Value.Date < today)).ToList();
    }
}