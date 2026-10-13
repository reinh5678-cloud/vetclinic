using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> SwitchByPinAsync(string pin, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionsAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken ct = default);
}