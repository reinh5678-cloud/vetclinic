using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Role> _roles;
    private readonly ISettingsService _settings;
    private readonly ICurrentUser _currentUser;

    public UserService(
        IRepository<User> users,
        IRepository<Role> roles,
        ISettingsService settings,
        ICurrentUser currentUser)
    {
        _users = users;
        _roles = roles;
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await _users.Query()
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        return users.Select(Map).ToList();
    }

    public async Task<AdminUserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var username = (request.Username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("El nombre de usuario es obligatorio.");

        var security = await _settings.GetSecurityAsync(ct);

        var password = request.Password ?? string.Empty;
        if (password.Length < security.PasswordMinLength)
            throw new ArgumentException($"La contraseña debe tener al menos {security.PasswordMinLength} caracteres.");

        if (await _users.Query().AnyAsync(u => u.Username == username, ct))
            throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");

        var role = await _roles.GetByIdAsync(request.RoleId, ct)
            ?? throw new ArgumentException("El rol seleccionado no existe.");

        var pin = NormalizePin(request.Pin, security.PinLength);
        if (pin is not null && await _users.Query().AnyAsync(u => u.PinAccess == pin, ct))
            throw new InvalidOperationException("Ese PIN ya está en uso por otro usuario.");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = (request.FullName ?? string.Empty).Trim(),
            Email = (request.Email ?? string.Empty).Trim(),
            RoleId = role.Id,
            PinAccess = pin,
            IsActive = true
        };

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);
        user.Role = role;
        return Map(user);
    }

    public async Task<AdminUserDto?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return null;

        var role = await _roles.GetByIdAsync(request.RoleId, ct)
            ?? throw new ArgumentException("El rol seleccionado no existe.");

        if (id == _currentUser.UserId)
        {
            if (!request.IsActive)
                throw new InvalidOperationException("No puedes desactivar tu propio usuario.");
            if (role.Id != user.RoleId)
                throw new InvalidOperationException("No puedes cambiar tu propio rol.");
        }

        user.FullName = (request.FullName ?? string.Empty).Trim();
        user.Email = (request.Email ?? string.Empty).Trim();
        user.RoleId = role.Id;
        user.IsActive = request.IsActive;

        _users.Update(user);
        await _users.SaveChangesAsync(ct);
        user.Role = role;
        return Map(user);
    }

    public async Task<bool> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null)
            return false;

        var security = await _settings.GetSecurityAsync(ct);
        var password = request.Password ?? string.Empty;
        if (password.Length < security.PasswordMinLength)
            throw new ArgumentException($"La contraseña debe tener al menos {security.PasswordMinLength} caracteres.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AdminUserDto?> SetPinAsync(int id, SetPinRequest request, CancellationToken ct = default)
    {
        var user = await _users.Query()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return null;

        var security = await _settings.GetSecurityAsync(ct);
        var pin = NormalizePin(request.Pin, security.PinLength);
        if (pin is not null && await _users.Query().AnyAsync(u => u.PinAccess == pin && u.Id != id, ct))
            throw new InvalidOperationException("Ese PIN ya está en uso por otro usuario.");

        user.PinAccess = pin;
        _users.Update(user);
        await _users.SaveChangesAsync(ct);
        return Map(user);
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

    private static AdminUserDto Map(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.RoleId,
        user.Role?.Name ?? string.Empty,
        user.IsActive,
        !string.IsNullOrEmpty(user.PinAccess));
}
