using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Domain;
using VetClinic.Domain.Entities;

namespace VetClinic.Application.Services;

public class RoleService : IRoleService
{
    private const int MaxNameLength = 50;
    private const string SystemRoleName = "Administrador";

    private readonly IRepository<Role> _roles;
    private readonly IRepository<User> _users;

    public RoleService(IRepository<Role> roles, IRepository<User> users)
    {
        _roles = roles;
        _users = users;
    }

    public async Task<IReadOnlyList<RoleDetailDto>> ListAsync(CancellationToken ct = default)
    {
        var roles = await _roles.Query().OrderBy(r => r.Name).ToListAsync(ct);
        var roleIds = await _users.Query().Select(u => u.RoleId).ToListAsync(ct);
        var counts = roleIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

        return roles.Select(r => Map(r, counts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<RoleDetailDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdAsync(id, ct);
        if (role is null)
            return null;

        var count = await _users.Query().CountAsync(u => u.RoleId == id, ct);
        return Map(role, count);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var name = NormalizeName(request.Name);
        if (await _roles.Query().AnyAsync(r => r.Name.ToLower() == name.ToLower(), ct))
            throw new InvalidOperationException("Ya existe un rol con ese nombre.");

        var permissions = NormalizePermissions(request.Permissions);

        var role = new Role
        {
            Name = name,
            PermissionsJson = JsonSerializer.Serialize(permissions)
        };

        await _roles.AddAsync(role, ct);
        await _roles.SaveChangesAsync(ct);
        return Map(role, 0);
    }

    public async Task<RoleDetailDto?> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdAsync(id, ct);
        if (role is null)
            return null;

        if (role.Name == SystemRoleName)
            throw new InvalidOperationException("El rol Administrador es del sistema y no puede modificarse.");

        var name = NormalizeName(request.Name);
        if (await _roles.Query().AnyAsync(r => r.Id != id && r.Name.ToLower() == name.ToLower(), ct))
            throw new InvalidOperationException("Ya existe un rol con ese nombre.");

        var permissions = NormalizePermissions(request.Permissions);

        role.Name = name;
        role.PermissionsJson = JsonSerializer.Serialize(permissions);
        _roles.Update(role);
        await _roles.SaveChangesAsync(ct);

        var count = await _users.Query().CountAsync(u => u.RoleId == id, ct);
        return Map(role, count);
    }

    private static string NormalizeName(string? name)
    {
        var value = (name ?? string.Empty).Trim();
        if (value.Length == 0)
            throw new ArgumentException("El nombre del rol es obligatorio.");
        if (value.Length > MaxNameLength)
            throw new ArgumentException($"El nombre del rol no puede superar los {MaxNameLength} caracteres.");
        return value;
    }

    private static string[] NormalizePermissions(IEnumerable<string>? permissions)
    {
        var requested = (permissions ?? Array.Empty<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct()
            .ToList();

        var invalid = requested.Where(p => !Permissions.All.Contains(p)).ToList();
        if (invalid.Count > 0)
            throw new ArgumentException($"Permisos inválidos: {string.Join(", ", invalid)}.");

        return Permissions.All.Where(requested.Contains).ToArray();
    }

    private static RoleDetailDto Map(Role role, int userCount)
        => new(role.Id, role.Name, Deserialize(role.PermissionsJson), userCount, role.Name == SystemRoleName);

    private static string[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
