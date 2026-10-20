using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VetClinic.Application.Common;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<User> _users;
    private readonly IConfiguration _configuration;
    private readonly ISettingsService _settings;
    private readonly IRecoveryLockout _lockout;

    public AuthService(IRepository<User> users, IConfiguration configuration, ISettingsService settings, IRecoveryLockout lockout)
    {
        _users = users;
        _configuration = configuration;
        _settings = settings;
        _lockout = lockout;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas");

        return await BuildResponseAsync(user, ct);
    }

    public async Task<AuthResponse> SwitchByPinAsync(string pin, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.PinAccess != null && u.PinAccess == pin, ct);

        return await BuildResponseAsync(user, ct);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(int userId, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == userId, ct);

        return GetPermissions(user?.Role);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await _users.Query()
            .Include(u => u.Role)
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        return users.Select(u => new UserSummaryDto(u.Id, u.FullName, u.Role?.Name ?? string.Empty)).ToList();
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        return MapProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        user.FullName = (request.FullName ?? string.Empty).Trim();
        user.Email = (request.Email ?? string.Empty).Trim();
        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        return MapProfile(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
            throw new UnauthorizedAccessException("La contraseña actual es incorrecta.");

        var security = await _settings.GetSecurityAsync(ct);
        var newPassword = request.NewPassword ?? string.Empty;
        if (newPassword.Length < security.PasswordMinLength)
            throw new ArgumentException($"La contraseña debe tener al menos {security.PasswordMinLength} caracteres.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task<UserProfileDto> ChangePinAsync(int userId, ChangePinRequest request, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        var currentPin = (request.CurrentPin ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(user.PinAccess) && currentPin != user.PinAccess)
            throw new UnauthorizedAccessException("El PIN actual es incorrecto.");

        var security = await _settings.GetSecurityAsync(ct);
        var newPin = NormalizePin(request.NewPin, security.PinLength);
        if (newPin is not null && await _users.Query().AnyAsync(u => u.PinAccess == newPin && u.Id != userId, ct))
            throw new InvalidOperationException("Ese PIN ya está en uso por otro usuario.");

        user.PinAccess = newPin;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        return MapProfile(user);
    }

    public async Task<RecoveryQuestionsResponse> GetRecoveryQuestionsAsync(string username, CancellationToken ct = default)
    {
        var name = (username ?? string.Empty).Trim();
        var user = await _users.Query().SingleOrDefaultAsync(u => u.Username == name, ct);
        var stored = ParseQuestions(user?.SecurityQuestionsJson);

        if (user is null || stored.Count == 0)
            throw new KeyNotFoundException("No se encontró el usuario o no tiene preguntas de seguridad configuradas.");

        return new RecoveryQuestionsResponse(
            user.Username,
            stored.Select(q => new RecoveryQuestionDto(q.QuestionId, SecurityQuestions.Text(q.QuestionId))).ToArray());
    }

    public async Task RecoverPasswordAsync(RecoverPasswordRequest request, CancellationToken ct = default)
    {
        var name = (request.Username ?? string.Empty).Trim();

        if (_lockout.IsLocked(name, out var remaining))
            throw new InvalidOperationException(
                $"Demasiados intentos fallidos. Vuelve a intentarlo en {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} minuto(s) o pide al administrador que restablezca tu contraseña.");

        var user = await _users.Query().SingleOrDefaultAsync(u => u.Username == name, ct);
        var stored = ParseQuestions(user?.SecurityQuestionsJson);
        var answers = request.Answers ?? Array.Empty<string>();

        var valid = user is not null && stored.Count > 0 && answers.Length == stored.Count;
        if (valid)
        {
            for (var i = 0; i < stored.Count; i++)
            {
                var provided = SecurityQuestions.NormalizeAnswer(answers[i]);
                if (provided.Length == 0 || !BCrypt.Net.BCrypt.Verify(provided, stored[i].AnswerHash))
                {
                    valid = false;
                    break;
                }
            }
        }

        if (!valid)
        {
            _lockout.RegisterFailure(name);
            throw new UnauthorizedAccessException("Las respuestas son incorrectas.");
        }

        var security = await _settings.GetSecurityAsync(ct);
        var newPassword = request.NewPassword ?? string.Empty;
        if (newPassword.Length < security.PasswordMinLength)
            throw new ArgumentException($"La contraseña debe tener al menos {security.PasswordMinLength} caracteres.");

        _lockout.Reset(name);
        user!.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
    }

    public async Task<MySecurityQuestionsResponse> GetMySecurityQuestionsAsync(int userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        return MapQuestions(user.SecurityQuestionsJson);
    }

    public async Task<MySecurityQuestionsResponse> SetSecurityQuestionsAsync(int userId, SetSecurityQuestionsRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
            throw new UnauthorizedAccessException("La contraseña actual es incorrecta.");

        var input = request.Questions ?? Array.Empty<SecurityQuestionAnswer>();
        if (input.Length != 2)
            throw new ArgumentException("Debes seleccionar exactamente 2 preguntas de seguridad.");
        if (input[0].QuestionId == input[1].QuestionId)
            throw new ArgumentException("Las preguntas seleccionadas deben ser distintas.");

        foreach (var item in input)
        {
            if (!SecurityQuestions.IsValid(item.QuestionId))
                throw new ArgumentException("Una de las preguntas seleccionadas no es válida.");
            if (SecurityQuestions.NormalizeAnswer(item.Answer).Length < 2)
                throw new ArgumentException("Cada respuesta debe tener al menos 2 caracteres.");
        }

        var stored = input
            .Select(i => new StoredSecurityQuestion(i.QuestionId, BCrypt.Net.BCrypt.HashPassword(SecurityQuestions.NormalizeAnswer(i.Answer))))
            .ToList();

        user.SecurityQuestionsJson = JsonSerializer.Serialize(stored);
        _users.Update(user);
        await _users.SaveChangesAsync(ct);

        return MapQuestions(user.SecurityQuestionsJson);
    }

    private static MySecurityQuestionsResponse MapQuestions(string? json) => new(
        ParseQuestions(json)
            .Select(q => new SecurityQuestionCatalogItem(q.QuestionId, SecurityQuestions.Text(q.QuestionId)))
            .ToArray());

    private static List<StoredSecurityQuestion> ParseQuestions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<StoredSecurityQuestion>();

        try
        {
            return JsonSerializer.Deserialize<List<StoredSecurityQuestion>>(json) ?? new List<StoredSecurityQuestion>();
        }
        catch (JsonException)
        {
            return new List<StoredSecurityQuestion>();
        }
    }

    private static string? NormalizePin(string? pin, int pinLength)
    {
        var value = (pin ?? string.Empty).Trim();
        if (value.Length == 0)
            return null;
        if (value.Length != pinLength)
            throw new ArgumentException($"El PIN debe tener exactamente {pinLength} dígitos.");
        return value;
    }

    private static UserProfileDto MapProfile(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.Role?.Name ?? string.Empty,
        !string.IsNullOrEmpty(user.PinAccess));

    private async Task<AuthResponse> BuildResponseAsync(User? user, CancellationToken ct)
    {
        if (user is null || user.Role is null)
            throw new UnauthorizedAccessException("PIN inválido");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Usuario desactivado");

        var security = await _settings.GetSecurityAsync(ct);
        var permissions = GetPermissions(user.Role);
        var token = GenerateToken(user, permissions, security.SessionHours);
        var dto = new UserDto(user.Id, user.Username, user.FullName, user.Email, user.Role.Name);
        return new AuthResponse(token, dto, permissions);
    }

    private static string[] GetPermissions(Role? role)
    {
        if (role is null || string.IsNullOrWhiteSpace(role.PermissionsJson))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<string[]>(role.PermissionsJson) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

private string GenerateToken(User user, string[] permissions, int expirationHours)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role!.Name)
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        if (expirationHours <= 0)
            expirationHours = 12;

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}