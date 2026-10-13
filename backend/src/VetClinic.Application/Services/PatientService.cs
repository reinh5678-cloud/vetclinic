using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Services;

public class PatientService : IPatientService
{
    private readonly IRepository<Tutor> _tutors;
    private readonly IRepository<Pet> _pets;

    public PatientService(IRepository<Tutor> tutors, IRepository<Pet> pets)
    {
        _tutors = tutors;
        _pets = pets;
    }

    public async Task<TutorDto> CreateTutorAsync(CreateTutorRequest r, CancellationToken ct = default)
    {
        ValidateTutorFields(r.FirstName, r.LastName);
        var tutor = new Tutor
        {
            FirstName = r.FirstName.Trim(),
            LastName = r.LastName.Trim(),
            Email = r.Email?.Trim() ?? string.Empty,
            Phone = r.Phone?.Trim() ?? string.Empty,
            Address = r.Address?.Trim() ?? string.Empty,
            DocumentNumber = string.IsNullOrWhiteSpace(r.DocumentNumber) ? null : r.DocumentNumber.Trim()
        };
        await _tutors.AddAsync(tutor, ct);
        await _tutors.SaveChangesAsync(ct);
        return Map(tutor);
    }

    public async Task<IReadOnlyList<TutorDto>> ListTutorsAsync(CancellationToken ct = default)
    {
        var tutors = await _tutors.Query().OrderBy(t => t.LastName).ThenBy(t => t.FirstName).ToListAsync(ct);
        return tutors.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<TutorListDto>> ListTutorsWithPetsAsync(CancellationToken ct = default)
    {
        var tutors = await _tutors.Query()
            .Include(t => t.Pets)
            .OrderBy(t => t.LastName).ThenBy(t => t.FirstName)
            .ToListAsync(ct);
        return tutors.Select(MapTutorList).ToList();
    }

    public async Task<TutorListDto?> GetTutorAsync(int id, CancellationToken ct = default)
    {
        var tutor = await _tutors.Query().Include(t => t.Pets).FirstOrDefaultAsync(t => t.Id == id, ct);
        return tutor is null ? null : MapTutorList(tutor);
    }

    public async Task<TutorListDto?> UpdateTutorAsync(int id, UpdateTutorRequest r, CancellationToken ct = default)
    {
        ValidateTutorFields(r.FirstName, r.LastName);
        var tutor = await _tutors.Query().Include(t => t.Pets).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tutor is null) return null;

        tutor.FirstName = r.FirstName.Trim();
        tutor.LastName = r.LastName.Trim();
        tutor.Email = r.Email?.Trim() ?? string.Empty;
        tutor.Phone = r.Phone?.Trim() ?? string.Empty;
        tutor.Address = r.Address?.Trim() ?? string.Empty;
        tutor.DocumentNumber = string.IsNullOrWhiteSpace(r.DocumentNumber) ? null : r.DocumentNumber.Trim();
        _tutors.Update(tutor);
        await _tutors.SaveChangesAsync(ct);
        return MapTutorList(tutor);
    }

    public async Task<PetDto> CreatePetAsync(CreatePetRequest r, CancellationToken ct = default)
    {
        await ValidatePetFieldsAsync(r.Name, r.WeightKg, r.TutorId, ct);
        var pet = new Pet
        {
            Name = r.Name.Trim(),
            Species = r.Species,
            Breed = r.Breed?.Trim() ?? string.Empty,
            BirthDate = r.BirthDate,
            Sex = r.Sex,
            WeightKg = r.WeightKg,
            Color = r.Color?.Trim() ?? string.Empty,
            TutorId = r.TutorId,
            IsSterilized = r.IsSterilized,
            PhotoUrl = string.IsNullOrWhiteSpace(r.PhotoUrl) ? null : r.PhotoUrl.Trim()
        };
        await _pets.AddAsync(pet, ct);
        await _pets.SaveChangesAsync(ct);
        return Map(pet);
    }

    public async Task<PetDto?> UpdatePetAsync(int id, UpdatePetRequest r, CancellationToken ct = default)
    {
        await ValidatePetFieldsAsync(r.Name, r.WeightKg, r.TutorId, ct);
        var pet = await _pets.GetByIdAsync(id, ct);
        if (pet is null) return null;

        pet.Name = r.Name.Trim();
        pet.Species = r.Species;
        pet.Breed = r.Breed?.Trim() ?? string.Empty;
        pet.BirthDate = r.BirthDate;
        pet.Sex = r.Sex;
        pet.WeightKg = r.WeightKg;
        pet.Color = r.Color?.Trim() ?? string.Empty;
        pet.TutorId = r.TutorId;
        pet.IsSterilized = r.IsSterilized;
        pet.PhotoUrl = string.IsNullOrWhiteSpace(r.PhotoUrl) ? null : r.PhotoUrl.Trim();
        _pets.Update(pet);
        await _pets.SaveChangesAsync(ct);
        return Map(pet);
    }

    public async Task<PetDto?> GetPetAsync(int id, CancellationToken ct = default)
    {
        var pet = await _pets.Query().Include(p => p.Tutor).FirstOrDefaultAsync(p => p.Id == id, ct);
        return pet is null ? null : Map(pet);
    }

    public async Task<IReadOnlyList<PetDto>> ListPetsAsync(string? search = null, Species? species = null, bool? active = null, CancellationToken ct = default)
    {
        var query = _pets.Query().Include(p => p.Tutor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Breed.ToLower().Contains(term) ||
                (p.Tutor != null && (p.Tutor.FirstName + " " + p.Tutor.LastName).ToLower().Contains(term)));
        }

        if (species.HasValue) query = query.Where(p => p.Species == species.Value);
        if (active.HasValue) query = query.Where(p => p.IsActive == active.Value);

        var pets = await query.OrderBy(p => p.Name).ToListAsync(ct);
        return pets.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PetDto>> ListPetsByTutorAsync(int tutorId, CancellationToken ct = default)
    {
        var pets = await _pets.Query().Where(p => p.TutorId == tutorId).Include(p => p.Tutor).OrderBy(p => p.Name).ToListAsync(ct);
        return pets.Select(Map).ToList();
    }

    public async Task<PetDto?> SetPetActiveAsync(int id, bool active, CancellationToken ct = default)
    {
        var pet = await _pets.Query().Include(p => p.Tutor).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pet is null) return null;

        pet.IsActive = active;
        _pets.Update(pet);
        await _pets.SaveChangesAsync(ct);
        return Map(pet);
    }

    private static void ValidateTutorFields(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre del tutor es obligatorio");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("El apellido del tutor es obligatorio");
    }

    private async Task ValidatePetFieldsAsync(string? name, decimal weightKg, int? tutorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del paciente es obligatorio");
        if (weightKg < 0)
            throw new ArgumentException("El peso no puede ser negativo");
        if (tutorId is int tid && !await _tutors.Query().AnyAsync(t => t.Id == tid, ct))
            throw new ArgumentException("El tutor seleccionado no existe");
    }

    private static TutorDto Map(Tutor t) => new(t.Id, t.FirstName, t.LastName, t.Email, t.Phone, t.Address, t.DocumentNumber);

    private static TutorListDto MapTutorList(Tutor t) => new(
        t.Id, t.FirstName, t.LastName, t.Email, t.Phone, t.Address, t.DocumentNumber ?? string.Empty, t.Pets.Count);

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return Math.Max(0, age);
    }

    private static PetDto Map(Pet p) => new(
        p.Id, p.Name, p.Species.ToString(), p.Breed, p.BirthDate, p.Sex.ToString(), p.WeightKg,
        p.Color, p.TutorId,
        p.BirthDate.HasValue ? (int?)CalculateAge(p.BirthDate.Value) : null,
        p.IsSterilized, p.PhotoUrl,
        p.Tutor is null ? null : $"{p.Tutor.FirstName} {p.Tutor.LastName}",
        p.IsActive);
}
