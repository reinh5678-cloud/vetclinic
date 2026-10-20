using VetClinic.Application.DTOs;

namespace VetClinic.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> SwitchByPinAsync(string pin, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetPermissionsAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<UserProfileDto> ChangePinAsync(int userId, ChangePinRequest request, CancellationToken ct = default);
    Task<RecoveryQuestionsResponse> GetRecoveryQuestionsAsync(string username, CancellationToken ct = default);
    Task RecoverPasswordAsync(RecoverPasswordRequest request, CancellationToken ct = default);
    Task<MySecurityQuestionsResponse> GetMySecurityQuestionsAsync(int userId, CancellationToken ct = default);
    Task<MySecurityQuestionsResponse> SetSecurityQuestionsAsync(int userId, SetSecurityQuestionsRequest request, CancellationToken ct = default);
}