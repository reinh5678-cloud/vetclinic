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
    [Fact]
    public async Task ListUpcomingAsync_Should_Return_Only_Upcoming_Appointments()
    {
        var appointmentsRepo = new Mock<IRepository<Appointment>>();
        var service = new AppointmentService(appointmentsRepo.Object);
        var upcoming = new Appointment { Id = 1, StartTime = DateTime.UtcNow.AddHours(1), Pet = new Pet { Name = "Fido" }, Vet = new User { FullName = "Dr. Smith" } };
        var old = new Appointment { Id = 2, StartTime = DateTime.UtcNow.AddDays(-10), Pet = new Pet { Name = "Rex" }, Vet = new User { FullName = "Dr. Smith" } };
        appointmentsRepo.Setup(r => r.Query()).Returns(new[] { old, upcoming }.BuildMockDbSet().Object);

        var result = await service.ListUpcomingAsync(CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal(1, only.Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_Reject_Invalid_Transition()
    {
        var app = new Appointment { Id = 1, Status = AppointmentStatus.Completada, StartTime = DateTime.UtcNow };
        var appointmentsRepo = new Mock<IRepository<Appointment>>();
        appointmentsRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        var service = new AppointmentService(appointmentsRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(1, new UpdateAppointmentStatusRequest(AppointmentStatus.EnEspera, string.Empty), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_Apply_Valid_Transition()
    {
        var app = new Appointment { Id = 1, Status = AppointmentStatus.Programada, StartTime = DateTime.UtcNow, Pet = new Pet { Name = "Fido" }, Vet = new User { FullName = "Dr" } };
        var appointmentsRepo = new Mock<IRepository<Appointment>>();
        appointmentsRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        appointmentsRepo.Setup(r => r.Query()).Returns(new[] { app }.BuildMockDbSet().Object);
        appointmentsRepo.Setup(r => r.Update(It.IsAny<Appointment>()));
        appointmentsRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var service = new AppointmentService(appointmentsRepo.Object);

        var result = await service.UpdateStatusAsync(1, new UpdateAppointmentStatusRequest(AppointmentStatus.EnEspera, string.Empty), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("EnEspera", result.Status);
    }
}
