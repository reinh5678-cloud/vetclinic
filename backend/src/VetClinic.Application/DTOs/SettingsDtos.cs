namespace VetClinic.Application.DTOs;

public record AdminUserDto(int Id, string Username, string FullName, string Email, int RoleId, string Role, bool IsActive, bool HasPin, bool HasSecurityQuestions);

public record RoleDto(int Id, string Name);

public record RoleDetailDto(int Id, string Name, string[] Permissions, int UserCount, bool IsSystem);

public record CreateRoleRequest(string Name, string[] Permissions);

public record UpdateRoleRequest(string Name, string[] Permissions);

public record PermissionDto(string Code, string Group, string Label);

public record CreateUserRequest(string Username, string Password, string FullName, string Email, int RoleId, string? Pin);

public record UpdateUserRequest(string FullName, string Email, int RoleId, bool IsActive);

public record ResetPasswordRequest(string Password);

public record SetPinRequest(string? Pin);

public record SystemSettingsDto(
    decimal TaxRate,
    double JwtExpirationHours,
    string AttachmentsPath,
    int MaxAttachmentMb,
    int ExpiringSoonDays,
    string Environment,
    string Version);

public record UpdateBillingRequest(decimal TaxRate);
