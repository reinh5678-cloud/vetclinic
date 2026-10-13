using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IHospitalizationService
{
    Task<HospitalizationDto> StartAsync(StartHospitalizationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<HospitalizationDto>> ListActiveAsync(CancellationToken ct = default);
    Task<HospitalizationDetailDto?> GetDetailAsync(int id, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> AddEvolutionAsync(int hospitalizationId, AddEvolutionRequest request, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> AddMedicationAsync(int hospitalizationId, AddMedicationAdministrationRequest request, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> DischargeAsync(int hospitalizationId, CancellationToken ct = default);
}