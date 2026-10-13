using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken ct = default);
    Task<AdminUserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<AdminUserDto?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default);
    Task<AdminUserDto?> SetPinAsync(int id, SetPinRequest request, CancellationToken ct = default);
}
