using VetClinic.Application.DTOs;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Interfaces;

public interface IPatientService
{
    Task<TutorDto> CreateTutorAsync(CreateTutorRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TutorDto>> ListTutorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TutorListDto>> ListTutorsWithPetsAsync(CancellationToken ct = default);
    Task<TutorListDto?> GetTutorAsync(int id, CancellationToken ct = default);
    Task<TutorListDto?> UpdateTutorAsync(int id, UpdateTutorRequest request, CancellationToken ct = default);
    Task<PetDto> CreatePetAsync(CreatePetRequest request, CancellationToken ct = default);
    Task<PetDto?> UpdatePetAsync(int id, UpdatePetRequest request, CancellationToken ct = default);
    Task<PetDto?> GetPetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PetDto>> ListPetsAsync(string? search = null, Species? species = null, bool? active = null, CancellationToken ct = default);
    Task<IReadOnlyList<PetDto>> ListPetsByTutorAsync(int tutorId, CancellationToken ct = default);
    Task<PetDto?> SetPetActiveAsync(int id, bool active, CancellationToken ct = default);
}
