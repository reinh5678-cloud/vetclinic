using VetClinic.Application.DTOs;
using VetClinic.Domain.Enums;

namespace VetClinic.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken ct = default);
    Task<AppointmentDto?> UpdateAsync(int id, UpdateAppointmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> ListAsync(DateTime? from, DateTime? to, int? vetId, AppointmentStatus? status, int? petId, CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> ListUpcomingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AppointmentDto>> ListByPetAsync(int petId, CancellationToken ct = default);
    Task<AppointmentDto?> UpdateStatusAsync(int id, UpdateAppointmentStatusRequest request, CancellationToken ct = default);
    Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
