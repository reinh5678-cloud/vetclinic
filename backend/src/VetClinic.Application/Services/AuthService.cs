using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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

    public AuthService(IRepository<User> users, IConfiguration configuration, ISettingsService settings)
    {
        _users = users;
        _configuration = configuration;
        _settings = settings;
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