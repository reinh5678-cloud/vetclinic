using Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;

namespace VetClinic.UnitTests;

public class UserServiceTests
{
    private static readonly Role AdminRole = new() { Id = 1, Name = "Administrador" };
    private static readonly Role VetRole = new() { Id = 2, Name = "Veterinario" };

    private static UserService CreateService(
        IEnumerable<User>? users = null,
        IEnumerable<Role>? roles = null,
        int? currentUserId = null,
        ISettingsService? settings = null)
    {
        var usersRepo = new Mock<IRepository<User>>();
        usersRepo.Setup(r => r.Query()).Returns((users ?? Array.Empty<User>()).ToList().BuildMockDbSet().Object);
        usersRepo.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        usersRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var rolesRepo = new Mock<IRepository<Role>>();
        var roleList = (roles ?? new[] { AdminRole, VetRole }).ToList();
        rolesRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => roleList.FirstOrDefault(r => r.Id == id));

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(c => c.UserId).Returns(currentUserId);

        return new UserService(usersRepo.Object, rolesRepo.Object, settings ?? new FakeSettingsService(), currentUser.Object);
    }

    private static User ExistingUser(int id, string username, int roleId, string? pin = null) => new()
    {
        Id = id,
        Username = username,
        FullName = username,
        Email = $"{username}@vet.local",
        RoleId = roleId,
        Role = roleId == AdminRole.Id ? AdminRole : VetRole,
        PinAccess = pin,
        IsActive = true
    };

    [Fact]
    public async Task CreateAsync_Should_Hash_Password_And_Map_Fields()
    {
        var service = CreateService();
        var request = new CreateUserRequest("nuevo", "Secreta1!", "Nuevo Usuario", "nuevo@vet.local", VetRole.Id, "5555");

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal("nuevo", result.Username);
        Assert.Equal("Nuevo Usuario", result.FullName);
        Assert.Equal("Veterinario", result.Role);
        Assert.True(result.IsActive);
        Assert.True(result.HasPin);
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Username()
    {
        var service = CreateService(users: new[] { ExistingUser(1, "admin", AdminRole.Id) });
        var request = new CreateUserRequest("admin", "Secreta1!", "Otro", "otro@vet.local", VetRole.Id, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Short_Password()
    {
        var service = CreateService();
        var request = new CreateUserRequest("nuevo", "123", "Nuevo", "nuevo@vet.local", VetRole.Id, null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Pin()
    {
        var service = CreateService(users: new[] { ExistingUser(1, "admin", AdminRole.Id, pin: "1111") });
        var request = new CreateUserRequest("nuevo", "Secreta1!", "Nuevo", "nuevo@vet.local", VetRole.Id, "1111");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Should_Not_Allow_Self_Deactivate()
    {
        var admin = ExistingUser(1, "admin", AdminRole.Id);
        var service = CreateService(users: new[] { admin }, currentUserId: admin.Id);
        var request = new UpdateUserRequest(admin.FullName, admin.Email, AdminRole.Id, false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(admin.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Should_Not_Allow_Self_Role_Change()
    {
        var admin = ExistingUser(1, "admin", AdminRole.Id);
        var service = CreateService(users: new[] { admin }, currentUserId: admin.Id);
        var request = new UpdateUserRequest(admin.FullName, admin.Email, VetRole.Id, true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(admin.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task SetPinAsync_Should_Reject_Duplicate_Pin()
    {
        var admin = ExistingUser(1, "admin", AdminRole.Id, pin: "1111");
        var vet = ExistingUser(2, "vet", VetRole.Id);
        var service = CreateService(users: new[] { admin, vet });
        var request = new SetPinRequest("1111");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetPinAsync(vet.Id, request, CancellationToken.None));
    }

    [Fact]
    public async Task SetPinAsync_Should_Clear_Pin_When_Empty()
    {
        var vet = ExistingUser(2, "vet", VetRole.Id, pin: "2222");
        var service = CreateService(users: new[] { vet });
        var request = new SetPinRequest("  ");

        var result = await service.SetPinAsync(vet.Id, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.HasPin);
    }
}
