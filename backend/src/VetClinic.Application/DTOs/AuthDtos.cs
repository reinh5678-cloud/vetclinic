namespace VetClinic.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginPinRequest(string Pin);

public record UserDto(int Id, string Username, string FullName, string Email, string Role);

public record UserSummaryDto(int Id, string FullName, string Role);

public record AuthResponse(string Token, UserDto User, string[] Permissions);

public record PermissionsResponse(string[] Permissions);