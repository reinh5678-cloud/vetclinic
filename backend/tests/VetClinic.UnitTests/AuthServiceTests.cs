using Microsoft.Extensions.Configuration;
using Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;

namespace VetClinic.UnitTests;

public class AuthServiceTests
{
    private static readonly Role VetRole = new() { Id = 2, Name = "Veterinario" };

    private static User ExistingUser(int id, string username, string? pin = null, string password = "Actual1!")
    {
        var user = new User
        {
            Id = id,
            Username = username,
            FullName = username,
            Email = $"{username}@vet.local",
            RoleId = VetRole.Id,
            Role = VetRole,
            PinAccess = pin,
            IsActive = true
        };
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        return user;
    }

    private static AuthService CreateService(IEnumerable<User>? users = null, ISettingsService? settings = null, IRecoveryLockout? lockout = null)
    {
        var userList = (users ?? Array.Empty<User>()).ToList();
        var repo = new Mock<IRepository<User>>();
        repo.Setup(r => r.Query()).Returns(() => userList.BuildMockDbSet().Object);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => userList.FirstOrDefault(u => u.Id == id));
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new AuthService(repo.Object, new Mock<IConfiguration>().Object, settings ?? new FakeSettingsService(), lockout ?? new RecoveryLockout());
    }

    [Fact]
    public async Task GetProfileAsync_Should_Map_User()
    {
        var user = ExistingUser(1, "vet", pin: "2222");
        var service = CreateService(new[] { user });

        var result = await service.GetProfileAsync(user.Id, CancellationToken.None);

        Assert.Equal("vet", result.Username);
        Assert.Equal("Veterinario", result.Role);
        Assert.True(result.HasPin);
    }

    [Fact]
    public async Task GetProfileAsync_Should_Throw_When_Not_Found()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetProfileAsync(99, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateProfileAsync_Should_Update_FullName_And_Email()
    {
        var user = ExistingUser(1, "vet");
        var service = CreateService(new[] { user });

        var result = await service.UpdateProfileAsync(1, new UpdateProfileRequest("  Nuevo Nombre ", " nuevo@vet.local "), CancellationToken.None);

        Assert.Equal("Nuevo Nombre", result.FullName);
        Assert.Equal("nuevo@vet.local", result.Email);
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Reject_Wrong_Current_Password()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ChangePasswordAsync(1, new ChangePasswordRequest("Incorrecta1!", "Nueva1!"), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Reject_Short_New_Password()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangePasswordAsync(1, new ChangePasswordRequest("Actual1!", "123"), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_Should_Update_Hash()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        var service = CreateService(new[] { user });

        await service.ChangePasswordAsync(1, new ChangePasswordRequest("Actual1!", "Nueva1!"), CancellationToken.None);

        Assert.True(BCrypt.Net.BCrypt.Verify("Nueva1!", user.PasswordHash));
    }

    [Fact]
    public async Task ChangePinAsync_Should_Reject_Wrong_Current_Pin()
    {
        var user = ExistingUser(1, "vet", pin: "2222");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ChangePinAsync(1, new ChangePinRequest("9999", "3333"), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePinAsync_Should_Reject_Wrong_Length()
    {
        var user = ExistingUser(1, "vet");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ChangePinAsync(1, new ChangePinRequest(null, "12"), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePinAsync_Should_Reject_Duplicate_Pin()
    {
        var admin = ExistingUser(1, "admin", pin: "1111");
        var vet = ExistingUser(2, "vet");
        var service = CreateService(new[] { admin, vet });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangePinAsync(vet.Id, new ChangePinRequest(null, "1111"), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePinAsync_Should_Clear_Pin_When_New_Empty()
    {
        var user = ExistingUser(1, "vet", pin: "2222");
        var service = CreateService(new[] { user });

        var result = await service.ChangePinAsync(1, new ChangePinRequest("2222", "  "), CancellationToken.None);

        Assert.False(result.HasPin);
    }

    [Fact]
    public async Task ChangePinAsync_Should_Allow_Pin_When_None_Set()
    {
        var user = ExistingUser(1, "vet");
        var service = CreateService(new[] { user });

        var result = await service.ChangePinAsync(1, new ChangePinRequest(null, "3333"), CancellationToken.None);

        Assert.True(result.HasPin);
    }

    private static string QuestionsJson(params (int Id, string Answer)[] answers) =>
        System.Text.Json.JsonSerializer.Serialize(
            answers
                .Select(a => new StoredSecurityQuestion(a.Id, BCrypt.Net.BCrypt.HashPassword(a.Answer.Trim().ToLowerInvariant())))
                .ToList());

    [Fact]
    public async Task GetRecoveryQuestionsAsync_Should_Return_Questions()
    {
        var user = ExistingUser(1, "vet");
        user.SecurityQuestionsJson = QuestionsJson((1, "luna"), (2, "barcelona"));
        var service = CreateService(new[] { user });

        var result = await service.GetRecoveryQuestionsAsync("vet", CancellationToken.None);

        Assert.Equal("vet", result.Username);
        Assert.Equal(2, result.Questions.Length);
        Assert.Equal(1, result.Questions[0].QuestionId);
        Assert.False(string.IsNullOrWhiteSpace(result.Questions[0].Text));
    }

    [Fact]
    public async Task GetRecoveryQuestionsAsync_Should_Throw_When_No_Questions()
    {
        var user = ExistingUser(1, "vet");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetRecoveryQuestionsAsync("vet", CancellationToken.None));
    }

    [Fact]
    public async Task GetRecoveryQuestionsAsync_Should_Throw_When_User_Missing()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetRecoveryQuestionsAsync("nadie", CancellationToken.None));
    }

    [Fact]
    public async Task RecoverPasswordAsync_Should_Update_Hash_With_Correct_Answers()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        user.SecurityQuestionsJson = QuestionsJson((1, "luna"), (2, "barcelona"));
        var service = CreateService(new[] { user });

        await service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "  Luna ", "BARCELONA" }, "Nueva123!"), CancellationToken.None);

        Assert.True(BCrypt.Net.BCrypt.Verify("Nueva123!", user.PasswordHash));
    }

    [Fact]
    public async Task RecoverPasswordAsync_Should_Reject_Wrong_Answer()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        user.SecurityQuestionsJson = QuestionsJson((1, "luna"), (2, "barcelona"));
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "luna", "madrid" }, "Nueva123!"), CancellationToken.None));

        Assert.True(BCrypt.Net.BCrypt.Verify("Actual1!", user.PasswordHash));
    }

    [Fact]
    public async Task RecoverPasswordAsync_Should_Reject_Short_New_Password()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        user.SecurityQuestionsJson = QuestionsJson((1, "luna"), (2, "barcelona"));
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "luna", "barcelona" }, "12"), CancellationToken.None));
    }

    [Fact]
    public async Task RecoverPasswordAsync_Should_Lock_After_Failed_Attempts()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        user.SecurityQuestionsJson = QuestionsJson((1, "luna"), (2, "barcelona"));
        var lockout = new RecoveryLockout();
        var service = CreateService(new[] { user }, lockout: lockout);

        for (var i = 0; i < RecoveryLockout.DefaultMaxAttempts; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "mal", "mal" }, "Nueva123!"), CancellationToken.None));
        }

        Assert.True(lockout.IsLocked("vet", out var remaining));
        Assert.True(remaining > TimeSpan.Zero);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "luna", "barcelona" }, "Nueva123!"), CancellationToken.None));

        Assert.True(BCrypt.Net.BCrypt.Verify("Actual1!", user.PasswordHash));
    }

    [Fact]
    public async Task RecoverPasswordAsync_Should_Honor_Configured_MaxAttempts()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        user.SecurityQuestionsJson = QuestionsJson((1, "luna"), (2, "barcelona"));
        var lockout = new RecoveryLockout();
        lockout.SetMaxAttempts(2);
        var service = CreateService(new[] { user }, lockout: lockout);

        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "mal", "mal" }, "Nueva123!"), CancellationToken.None));
        }

        Assert.True(lockout.IsLocked("vet", out var remaining));
        Assert.True(remaining > TimeSpan.Zero);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecoverPasswordAsync(new RecoverPasswordRequest("vet", new[] { "luna", "barcelona" }, "Nueva123!"), CancellationToken.None));

        Assert.True(BCrypt.Net.BCrypt.Verify("Actual1!", user.PasswordHash));
    }

    [Fact]
    public void SetMaxAttempts_Should_Clamp_To_Valid_Range()
    {
        var lockout = new RecoveryLockout();

        lockout.SetMaxAttempts(0);
        Assert.Equal(RecoveryLockout.MinMaxAttempts, lockout.MaxAttempts);

        lockout.SetMaxAttempts(99);
        Assert.Equal(RecoveryLockout.MaxMaxAttempts, lockout.MaxAttempts);

        lockout.SetMaxAttempts(7);
        Assert.Equal(7, lockout.MaxAttempts);
    }

    [Fact]
    public async Task SetSecurityQuestionsAsync_Should_Reject_Wrong_Current_Password()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetSecurityQuestionsAsync(1, new SetSecurityQuestionsRequest("Incorrecta1!", new[]
            {
                new SecurityQuestionAnswer(1, "luna"),
                new SecurityQuestionAnswer(2, "barcelona")
            }), CancellationToken.None));
    }

    [Fact]
    public async Task SetSecurityQuestionsAsync_Should_Reject_Repeated_Questions()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        var service = CreateService(new[] { user });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetSecurityQuestionsAsync(1, new SetSecurityQuestionsRequest("Actual1!", new[]
            {
                new SecurityQuestionAnswer(1, "luna"),
                new SecurityQuestionAnswer(1, "barcelona")
            }), CancellationToken.None));
    }

    [Fact]
    public async Task SetSecurityQuestionsAsync_Should_Store_Hashes()
    {
        var user = ExistingUser(1, "vet", password: "Actual1!");
        var service = CreateService(new[] { user });

        var result = await service.SetSecurityQuestionsAsync(1, new SetSecurityQuestionsRequest("Actual1!", new[]
        {
            new SecurityQuestionAnswer(1, "  Luna "),
            new SecurityQuestionAnswer(3, "Gomez")
        }), CancellationToken.None);

        Assert.Equal(2, result.Questions.Length);
        Assert.Contains(1, result.Questions.Select(q => q.QuestionId));
        Assert.Contains(3, result.Questions.Select(q => q.QuestionId));
        Assert.DoesNotContain("luna", user.SecurityQuestionsJson!.ToLowerInvariant());
        Assert.DoesNotContain("gomez", user.SecurityQuestionsJson!.ToLowerInvariant());
        Assert.StartsWith("[", user.SecurityQuestionsJson!.TrimStart());
    }
}
