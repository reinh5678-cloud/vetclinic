using System.Text.Json;
using Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain;
using VetClinic.Domain.Entities;

namespace VetClinic.UnitTests;

public class RoleServiceTests
{
    private const int AdminRoleId = 1;
    private const int VetRoleId = 2;

    private static Role NewAdminRole() => new()
    {
        Id = AdminRoleId,
        Name = "Administrador",
        PermissionsJson = JsonSerializer.Serialize(Permissions.All)
    };

    private static Role NewVetRole() => new()
    {
        Id = VetRoleId,
        Name = "Veterinario",
        PermissionsJson = JsonSerializer.Serialize(new[] { Permissions.PatientsView })
    };

    private static RoleService CreateService(IEnumerable<Role>? roles = null, IEnumerable<User>? users = null)
    {
        var roleList = (roles ?? new[] { NewAdminRole(), NewVetRole() }).ToList();

        var rolesRepo = new Mock<IRepository<Role>>();
        rolesRepo.Setup(r => r.Query()).Returns(roleList.BuildMockDbSet().Object);
        rolesRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => roleList.FirstOrDefault(r => r.Id == id));
        rolesRepo.Setup(r => r.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role r, CancellationToken _) => r);
        rolesRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var usersRepo = new Mock<IRepository<User>>();
        usersRepo.Setup(r => r.Query()).Returns((users ?? Array.Empty<User>()).ToList().BuildMockDbSet().Object);

        return new RoleService(rolesRepo.Object, usersRepo.Object);
    }

    [Fact]
    public async Task CreateAsync_Should_Persist_Role_With_Selected_Permissions()
    {
        var service = CreateService();
        var request = new CreateRoleRequest("Peluquero", new[] { Permissions.PatientsView, Permissions.InvoicesView });

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("Peluquero", result.Name);
        Assert.Equal(new[] { Permissions.PatientsView, Permissions.InvoicesView }, result.Permissions);
        Assert.False(result.IsSystem);
        Assert.Equal(0, result.UserCount);
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Name_Case_Insensitive()
    {
        var service = CreateService();
        var request = new CreateRoleRequest("veterinario", new[] { Permissions.PatientsView });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Unknown_Permissions()
    {
        var service = CreateService();
        var request = new CreateRoleRequest("Rol raro", new[] { "hack.all" });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Empty_Name()
    {
        var service = CreateService();
        var request = new CreateRoleRequest("   ", new[] { Permissions.PatientsView });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Should_Not_Modify_System_Role()
    {
        var service = CreateService();
        var request = new UpdateRoleRequest("Administrador", new[] { Permissions.PatientsView });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(AdminRoleId, request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Should_Change_Name_And_Permissions()
    {
        var service = CreateService();
        var request = new UpdateRoleRequest("Veterinario Senior", new[] { Permissions.PatientsView, Permissions.MedicalView });

        var result = await service.UpdateAsync(VetRoleId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Veterinario Senior", result!.Name);
        Assert.Equal(new[] { Permissions.PatientsView, Permissions.MedicalView }, result.Permissions);
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_Null_When_Role_Missing()
    {
        var service = CreateService();
        var request = new UpdateRoleRequest("Otro", new[] { Permissions.PatientsView });

        var result = await service.UpdateAsync(999, request, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_Should_Report_User_Count_And_Permissions()
    {
        var user = new User { Id = 1, Username = "vet", FullName = "Vet", RoleId = VetRoleId, IsActive = true };
        var service = CreateService(users: new[] { user });

        var result = await service.ListAsync(CancellationToken.None);

        var admin = result.Single(r => r.Name == "Administrador");
        var vet = result.Single(r => r.Name == "Veterinario");
        Assert.True(admin.IsSystem);
        Assert.Equal(Permissions.All.Length, admin.Permissions.Length);
        Assert.Equal(1, vet.UserCount);
        Assert.Equal(0, admin.UserCount);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Null_When_Missing()
    {
        var service = CreateService();

        var result = await service.GetAsync(999, CancellationToken.None);

        Assert.Null(result);
    }
}
