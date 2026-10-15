using VetClinic.Application.DTOs;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Interfaces;

public interface IHospitalizationService
{
    Task<HospitalizationDto> StartAsync(StartHospitalizationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<HospitalizationDto>> ListActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HospitalizationDto>> ListAsync(HospitalizationStatus? status, int? petId, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> GetDetailAsync(int id, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> AddEvolutionAsync(int hospitalizationId, AddEvolutionRequest request, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> AddMedicationAsync(int hospitalizationId, AddMedicationAdministrationRequest request, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> ChangeStatusAsync(int hospitalizationId, HospitalizationStatus status, CancellationToken ct = default);
    Task<HospitalizationDetailDto?> DischargeAsync(int hospitalizationId, CancellationToken ct = default);
}