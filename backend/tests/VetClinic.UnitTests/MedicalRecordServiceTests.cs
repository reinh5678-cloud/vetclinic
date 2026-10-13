using Moq;
using MockQueryable.Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;

namespace VetClinic.UnitTests;

public class MedicalRecordServiceTests
{
    [Fact]
    public async Task ListByPetAsync_Should_Return_Records()
    {
        var records = new Mock<IRepository<MedicalRecord>>();
        var prescriptions = new Mock<IRepository<Prescription>>();
        var labResults = new Mock<IRepository<LabResult>>();
        var attachments = new Mock<IRepository<Attachment>>();
        var fileStorage = new Mock<IFileStorage>();
        var service = new MedicalRecordService(records.Object, prescriptions.Object, labResults.Object, attachments.Object, fileStorage.Object);
        records.Setup(r => r.Query()).Returns(new[]
        {
            new MedicalRecord { Id = 1, PetId = 1, Vet = new User { FullName = "Dr" }, Prescriptions = new List<Prescription>(), LabResults = new List<LabResult>(), Attachments = new List<Attachment>() },
            new MedicalRecord { Id = 2, PetId = 99, Vet = new User { FullName = "Other" }, Prescriptions = new List<Prescription>(), LabResults = new List<LabResult>(), Attachments = new List<Attachment>() }
        }.BuildMockDbSet().Object);

        var result = await service.ListByPetAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        var only = Assert.Single(result);
        Assert.Equal(1, only.Id);
    }
}
