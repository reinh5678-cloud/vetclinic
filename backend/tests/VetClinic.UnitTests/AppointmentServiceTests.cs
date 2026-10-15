using Moq;
using MockQueryable.Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.UnitTests;

public class AppointmentServiceTests
{
    private static (AppointmentService Service, Mock<IRepository<Appointment>> Appointments) Build(
        IEnumerable<Appointment>? appointments = null,
        IEnumerable<Pet>? pets = null,
        IEnumerable<User>? users = null,
        AgendaSettingsDto? agenda = null)
    {
        var apptList = (appointments ?? Array.Empty<Appointment>()).ToList();
        var petList = (pets ?? Array.Empty<Pet>()).ToList();
        var userList = (users ?? Array.Empty<User>()).ToList();

        var apptRepo = new Mock<IRepository<Appointment>>();
        apptRepo.Setup(r => r.Query()).Returns(() => apptList.BuildMockDbSet().Object);
        apptRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => apptList.FirstOrDefault(a => a.Id == id));
        apptRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Callback<Appointment, CancellationToken>((a, _) => { a.Id = apptList.Count + 1; apptList.Add(a); })
            .ReturnsAsync((Appointment a, CancellationToken _) => a);
        apptRepo.Setup(r => r.Update(It.IsAny<Appointment>()));
        apptRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var petRepo = new Mock<IRepository<Pet>>();
        petRepo.Setup(r => r.Query()).Returns(() => petList.BuildMockDbSet().Object);

        var userRepo = new Mock<IRepository<User>>();
        userRepo.Setup(r => r.Query()).Returns(() => userList.BuildMockDbSet().Object);

        var settings = new FakeSettingsService
        {
            Agenda = agenda ?? new AgendaSettingsDto("00:00", "00:00", 30, 30, 3650, Array.Empty<string>())
        };

        return (new AppointmentService(apptRepo.Object, petRepo.Object, userRepo.Object, settings), apptRepo);
    }

    private static readonly AgendaSettingsDto WorkingAgenda =
        new("08:00", "20:00", 30, 30, 90, new[] { "Sunday" });

    private static DateTime NextWeekday(int hour, int minute = 0)
    {
        var d = DateTime.UtcNow.Date.AddDays(1).AddHours(hour).AddMinutes(minute);
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            d = d.AddDays(1);
        return d;
    }

    private static DateTime NextDayOfWeek(DayOfWeek day, int hour)
    {
        var d = DateTime.UtcNow.Date.AddDays(1).AddHours(hour);
        while (d.DayOfWeek != day)
            d = d.AddDays(1);
        return d;
    }

    private static User Vet(int id = 5) => new() { Id = id, FullName = "Dra. Vet", IsActive = true, Role = new Role { Name = "Veterinario" } };
    private static Pet Pet(int id = 1) => new() { Id = id, Name = "Rocky" };

    [Fact]
    public async Task ListUpcomingAsync_Should_Return_Only_Upcoming_Appointments()
    {
        var upcoming = new Appointment { Id = 1, StartTime = DateTime.UtcNow.AddHours(1), Pet = new Pet { Name = "Fido" }, Vet = new User { FullName = "Dr. Smith" } };
        var old = new Appointment { Id = 2, StartTime = DateTime.UtcNow.AddDays(-10), Pet = new Pet { Name = "Rex" }, Vet = new User { FullName = "Dr. Smith" } };
        var (service, _) = Build(new[] { old, upcoming });

        var result = await service.ListUpcomingAsync();

        var only = Assert.Single(result);
        Assert.Equal(1, only.Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_Reject_Invalid_Transition()
    {
        var app = new Appointment { Id = 1, Status = AppointmentStatus.Completada, StartTime = DateTime.UtcNow };
        var (service, _) = Build(new[] { app });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(1, new UpdateAppointmentStatusRequest(AppointmentStatus.EnEspera, null)));
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_Apply_Valid_Transition_And_Save_Notes()
    {
        var app = new Appointment { Id = 1, Status = AppointmentStatus.Programada, StartTime = DateTime.UtcNow, Pet = new Pet { Name = "Fido" }, Vet = new User { FullName = "Dr" } };
        var (service, _) = Build(new[] { app });

        var result = await service.UpdateStatusAsync(1, new UpdateAppointmentStatusRequest(AppointmentStatus.Cancelada, "Cliente no pudo"));

        Assert.NotNull(result);
        Assert.Equal("Cancelada", result!.Status);
        Assert.Equal("Cliente no pudo", result.Notes);
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Past_Start()
    {
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { Vet() });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateAppointmentRequest(DateTime.UtcNow.AddHours(-1), 30, UrgencyLevel.Baja, "Control", 1, 5)));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Overlap_With_Active_Status()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var existing = new Appointment { Id = 1, VetId = 5, Status = AppointmentStatus.EnEspera, StartTime = start, DurationMinutes = 30 };
        var (service, _) = Build(new[] { existing }, pets: new[] { Pet() }, users: new[] { Vet() });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateAppointmentRequest(start.AddMinutes(10), 30, UrgencyLevel.Media, "Control", 1, 5)));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_NonVeterinario_User()
    {
        var reception = new User { Id = 9, IsActive = true, Role = new Role { Name = "Recepcionista" } };
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { reception });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateAppointmentRequest(DateTime.UtcNow.AddDays(1), 30, UrgencyLevel.Baja, "Control", 1, 9)));
    }

    [Fact]
    public async Task CreateAsync_Should_Succeed_With_Valid_Data()
    {
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { Vet() });

        var result = await service.CreateAsync(
            new CreateAppointmentRequest(DateTime.UtcNow.AddDays(1), 45, UrgencyLevel.Alta, "  Urgencia  ", 1, 5));

        Assert.Equal("Programada", result.Status);
        Assert.Equal(45, result.DurationMinutes);
        Assert.Equal("Urgencia", result.Reason);
        Assert.Equal(5, result.VetId);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Overlap_Excluding_Self()
    {
        var baseTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var self = new Appointment { Id = 1, VetId = 5, PetId = 1, Status = AppointmentStatus.Programada, StartTime = baseTime, DurationMinutes = 30 };
        var other = new Appointment { Id = 2, VetId = 5, PetId = 1, Status = AppointmentStatus.Programada, StartTime = baseTime.AddHours(1), DurationMinutes = 30 };
        var (service, _) = Build(new[] { self, other }, pets: new[] { Pet() }, users: new[] { Vet() });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(1, new UpdateAppointmentRequest(baseTime.AddHours(1), 30, UrgencyLevel.Baja, "Control", 1, 5)));
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Vet_And_Status()
    {
        var a = new Appointment { Id = 1, VetId = 5, Status = AppointmentStatus.Programada, StartTime = DateTime.UtcNow.AddDays(1) };
        var b = new Appointment { Id = 2, VetId = 5, Status = AppointmentStatus.Cancelada, StartTime = DateTime.UtcNow.AddDays(1) };
        var c = new Appointment { Id = 3, VetId = 6, Status = AppointmentStatus.Programada, StartTime = DateTime.UtcNow.AddDays(1) };
        var (service, _) = Build(new[] { a, b, c });

        var result = await service.ListAsync(null, null, 5, AppointmentStatus.Programada, null);

        var only = Assert.Single(result);
        Assert.Equal(1, only.Id);
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Outside_Opening_Hours()
    {
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { Vet() }, agenda: WorkingAgenda);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateAppointmentRequest(NextWeekday(21), 30, UrgencyLevel.Baja, "Control", 1, 5)));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Closed_Weekday()
    {
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { Vet() }, agenda: WorkingAgenda);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateAppointmentRequest(NextDayOfWeek(DayOfWeek.Sunday, 10), 30, UrgencyLevel.Baja, "Control", 1, 5)));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Beyond_Max_Advance_Days()
    {
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { Vet() }, agenda: WorkingAgenda);

        var beyond = DateTime.UtcNow.Date.AddDays(100).AddHours(10);
        while (beyond.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            beyond = beyond.AddDays(1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateAppointmentRequest(beyond, 30, UrgencyLevel.Baja, "Control", 1, 5)));
    }

    [Fact]
    public async Task CreateAsync_Should_Accept_Slot_At_Opening_Boundary()
    {
        var (service, _) = Build(Array.Empty<Appointment>(), pets: new[] { Pet() }, users: new[] { Vet() }, agenda: WorkingAgenda);

        var result = await service.CreateAsync(
            new CreateAppointmentRequest(NextWeekday(8), 30, UrgencyLevel.Baja, "Control", 1, 5));

        Assert.Equal("Programada", result.Status);
    }
}
