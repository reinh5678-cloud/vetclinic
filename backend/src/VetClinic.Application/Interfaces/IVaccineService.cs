using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IVaccineService
{
    Task<VaccineApplicationDto?> AddAsync(int petId, AddVaccineRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<VaccineApplicationDto>> ListByPetAsync(int petId, CancellationToken ct = default);
    Task<IReadOnlyList<UpcomingVaccineDto>> ListUpcomingAsync(int days = 30, CancellationToken ct = default);
}