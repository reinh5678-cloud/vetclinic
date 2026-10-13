using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDetailDto>> ListAsync(CancellationToken ct = default);
    Task<RoleDetailDto?> GetAsync(int id, CancellationToken ct = default);
    Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDetailDto?> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct = default);
}
