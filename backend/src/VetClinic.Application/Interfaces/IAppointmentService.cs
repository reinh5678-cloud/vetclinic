using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> ListUpcomingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> ListByPetAsync(int petId, CancellationToken ct = default);
    Task<AppointmentDto?> UpdateStatusAsync(int id, UpdateAppointmentStatusRequest request, CancellationToken ct = default);
    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default);
}