namespace VetClinic.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginPinRequest(string Pin);

public record UserDto(int Id, string Username, string FullName, string Email, string Role);

public record UserSummaryDto(int Id, string FullName, string Role);

public record AuthResponse(string Token, UserDto User, string[] Permissions);

public record PermissionsResponse(string[] Permissions);

public record UserProfileDto(int Id, string Username, string FullName, string Email, string Role, bool HasPin);

public record UpdateProfileRequest(string FullName, string Email);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ChangePinRequest(string? CurrentPin, string? NewPin);

public record SecurityQuestionCatalogItem(int QuestionId, string Text);

public record StoredSecurityQuestion(int QuestionId, string AnswerHash);

public record RecoveryQuestionDto(int QuestionId, string Text);

public record RecoveryQuestionsResponse(string Username, RecoveryQuestionDto[] Questions);

public record RecoverPasswordRequest(string Username, string[] Answers, string NewPassword);

public record SecurityQuestionAnswer(int QuestionId, string Answer);

public record SetSecurityQuestionsRequest(string CurrentPassword, SecurityQuestionAnswer[] Questions);

public record MySecurityQuestionsResponse(SecurityQuestionCatalogItem[] Questions);