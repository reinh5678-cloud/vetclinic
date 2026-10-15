using Moq;
using MockQueryable.Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Common;
using VetClinic.Domain.Entities;

namespace VetClinic.UnitTests;

public class MedicalRecordServiceTests
{
    private sealed record Harness(
        MedicalRecordService Service,
        Mock<IRepository<MedicalRecord>> Records,
        Mock<IRepository<Prescription>> Prescriptions,
        Mock<IRepository<LabResult>> LabResults,
        Mock<IRepository<Attachment>> Attachments,
        Mock<IFileStorage> FileStorage,
        List<MedicalRecord> RecordList);

    private static Mock<IRepository<T>> Repo<T>(List<T> data) where T : BaseEntity
    {
        var repo = new Mock<IRepository<T>>();
        repo.Setup(r => r.Query()).Returns(() => data.BuildMockDbSet().Object);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => data.FirstOrDefault(e => e.Id == id));
        repo.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => data.Add(entity))
            .ReturnsAsync((T entity, CancellationToken _) => entity);
        repo.Setup(r => r.Update(It.IsAny<T>()));
        repo.Setup(r => r.Remove(It.IsAny<T>())).Callback<T>(entity => data.Remove(entity));
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return repo;
    }

    private static Harness Build(
        IEnumerable<MedicalRecord>? records = null,
        IEnumerable<Pet>? pets = null,
        IEnumerable<User>? users = null,
        IEnumerable<Appointment>? appointments = null,
        IEnumerable<Prescription>? prescriptions = null,
        IEnumerable<LabResult>? labResults = null,
        IEnumerable<Attachment>? attachments = null)
    {
        var recordList = (records ?? Array.Empty<MedicalRecord>()).ToList();
        var petList = (pets ?? Array.Empty<Pet>()).ToList();
        var userList = (users ?? Array.Empty<User>()).ToList();
        var appointmentList = (appointments ?? Array.Empty<Appointment>()).ToList();
        var prescriptionList = (prescriptions ?? Array.Empty<Prescription>()).ToList();
        var labResultList = (labResults ?? Array.Empty<LabResult>()).ToList();
        var attachmentList = (attachments ?? Array.Empty<Attachment>()).ToList();

        var recordRepo = Repo(recordList);
        recordRepo.Setup(r => r.AddAsync(It.IsAny<MedicalRecord>(), It.IsAny<CancellationToken>()))
            .Callback<MedicalRecord, CancellationToken>((m, _) => { m.Id = recordList.Count + 1; recordList.Add(m); })
            .ReturnsAsync((MedicalRecord m, CancellationToken _) => m);

        var prescriptionRepo = Repo(prescriptionList);
        var labResultRepo = Repo(labResultList);
        var attachmentRepo = Repo(attachmentList);

        var fileStorage = new Mock<IFileStorage>();
        fileStorage.Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string folder, string name, Stream _, CancellationToken _) => $"{folder}/{name}");
        fileStorage.Setup(s => s.GetPhysicalPath(It.IsAny<string?>())).Returns((string? p) => p);

        var service = new MedicalRecordService(
            recordRepo.Object,
            prescriptionRepo.Object,
            labResultRepo.Object,
            attachmentRepo.Object,
            Repo(petList).Object,
            Repo(userList).Object,
            Repo(appointmentList).Object,
            fileStorage.Object);

        return new Harness(service, recordRepo, prescriptionRepo, labResultRepo, attachmentRepo, fileStorage, recordList);
    }

    private static User Vet(int id = 5, bool active = true) =>
        new() { Id = id, FullName = "Dra. Vet", IsActive = active, Role = new Role { Name = "Veterinario" } };

    private static Pet Pet(int id = 1) => new() { Id = id, Name = "Rocky" };

    private static MedicalRecord Record(int id = 1, int petId = 1, int? vetId = 5, int? appointmentId = null) =>
        new()
        {
            Id = id,
            PetId = petId,
            VetId = vetId,
            AppointmentId = appointmentId,
            Symptoms = "Tos",
            Prescriptions = new List<Prescription>(),
            LabResults = new List<LabResult>(),
            Attachments = new List<Attachment>()
        };

    private static AddMedicalRecordRequest NewRecord(
        int petId = 1, int? vetId = 5, int? appointmentId = null,
        string symptoms = "Tos", decimal? weight = 10m, decimal? temp = 38m) =>
        new(petId, vetId, appointmentId, symptoms, "", "", "", weight, temp);

    private static UpdateMedicalRecordRequest UpdateRecord(
        decimal? weight = 10m, decimal? temp = 38m, int? vetId = null) =>
        new("Tos", "", "", "", weight, temp, null, vetId);

    [Fact]
    public async Task ListByPetAsync_Should_Return_Only_Records_For_Pet()
    {
        var h = Build(records: new[] { Record(1, petId: 1), Record(2, petId: 99) });

        var result = await h.Service.ListByPetAsync(1, CancellationToken.None);

        var only = Assert.Single(result);
        Assert.Equal(1, only.Id);
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Unknown_Pet()
    {
        var h = Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(petId: 1), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Unknown_Vet()
    {
        var h = Build(pets: new[] { Pet() });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(vetId: 5), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Inactive_Vet()
    {
        var h = Build(pets: new[] { Pet() }, users: new[] { Vet(active: false) });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(vetId: 5), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Out_Of_Range_Weight()
    {
        var h = Build(pets: new[] { Pet() }, users: new[] { Vet() });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(weight: 500m), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Out_Of_Range_Temperature()
    {
        var h = Build(pets: new[] { Pet() }, users: new[] { Vet() });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(temp: 80m), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Empty_Symptoms()
    {
        var h = Build(pets: new[] { Pet() }, users: new[] { Vet() });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(symptoms: "   "), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Trim_And_Persist()
    {
        var h = Build(pets: new[] { Pet() }, users: new[] { Vet() });

        var result = await h.Service.AddAsync(NewRecord(symptoms: "  Tos seca  "), CancellationToken.None);

        Assert.Equal("Tos seca", result.Symptoms);
        Assert.Equal(1, result.PetId);
        Assert.Equal(5, result.VetId);
        Assert.Single(h.RecordList);
    }

    [Fact]
    public async Task AddAsync_Should_Link_Appointment()
    {
        var appointment = new Appointment { Id = 7, PetId = 1 };
        var h = Build(pets: new[] { Pet() }, appointments: new[] { appointment });

        await h.Service.AddAsync(NewRecord(vetId: null, appointmentId: 7), CancellationToken.None);

        Assert.Equal(7, h.RecordList.Single().AppointmentId);
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Appointment_For_Other_Pet()
    {
        var appointment = new Appointment { Id = 7, PetId = 2 };
        var h = Build(pets: new[] { Pet(1) }, appointments: new[] { appointment });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAsync(NewRecord(petId: 1, vetId: null, appointmentId: 7), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_Should_Reject_Appointment_Already_Linked()
    {
        var appointment = new Appointment { Id = 7, PetId = 1 };
        var h = Build(
            records: new[] { Record(1, appointmentId: 7) },
            pets: new[] { Pet() },
            appointments: new[] { appointment });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.AddAsync(NewRecord(vetId: null, appointmentId: 7), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Should_Return_Null_For_Missing_Record()
    {
        var h = Build();

        var result = await h.Service.UpdateAsync(999, UpdateRecord(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Fields()
    {
        var h = Build(records: new[] { Record(1) }, users: new[] { Vet(6) });

        var result = await h.Service.UpdateAsync(1, UpdateRecord(weight: 12.5m, temp: 39m, vetId: 6), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(12.5m, result!.WeightKg);
        Assert.Equal(6, result.VetId);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Out_Of_Range_Temperature()
    {
        var h = Build(records: new[] { Record(1) });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.UpdateAsync(1, UpdateRecord(temp: 80m), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_Record_And_Files()
    {
        var record = Record(1);
        record.Attachments.Add(new Attachment { Id = 3, MedicalRecordId = 1, FilePath = "1/x.pdf" });
        var h = Build(records: new[] { record });

        var ok = await h.Service.DeleteAsync(1, CancellationToken.None);

        Assert.True(ok);
        Assert.Empty(h.RecordList);
        h.FileStorage.Verify(f => f.Delete("1/x.pdf"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_False_For_Missing_Record()
    {
        var h = Build();

        Assert.False(await h.Service.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task AddPrescriptionAsync_Should_Return_Null_For_Missing_Record()
    {
        var h = Build();

        var result = await h.Service.AddPrescriptionAsync(1, new AddPrescriptionRequest("Med", "", "", "", ""), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddPrescriptionAsync_Should_Trim_And_Add()
    {
        var h = Build(records: new[] { Record(1) });

        var result = await h.Service.AddPrescriptionAsync(
            1, new AddPrescriptionRequest("  Amoxicilina  ", "250mg", "c/12h", "7d", ""), CancellationToken.None);

        Assert.NotNull(result);
        h.Prescriptions.Verify(p => p.AddAsync(
            It.Is<Prescription>(x => x.MedicationName == "Amoxicilina" && x.MedicalRecordId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePrescriptionAsync_Should_Return_Null_For_Missing()
    {
        var h = Build(records: new[] { Record(1) });

        var result = await h.Service.UpdatePrescriptionAsync(
            1, 99, new UpdatePrescriptionRequest("X", "", "", "", ""), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddLabResultAsync_Should_Add_Result()
    {
        var h = Build(records: new[] { Record(1) });

        var result = await h.Service.AddLabResultAsync(
            1, new AddLabResultRequest("Hemograma", "OK", "4-10", false), CancellationToken.None);

        Assert.NotNull(result);
        h.LabResults.Verify(l => l.AddAsync(
            It.Is<LabResult>(x => x.TestType == "Hemograma" && x.MedicalRecordId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAttachmentFileAsync_Should_Reject_Disallowed_Extension()
    {
        var h = Build(records: new[] { Record(1) });
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAttachmentFileAsync(1, "virus.exe", "application/octet-stream", 3, stream, CancellationToken.None));
    }

    [Fact]
    public async Task AddAttachmentFileAsync_Should_Reject_Empty_File()
    {
        var h = Build(records: new[] { Record(1) });
        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddAttachmentFileAsync(1, "xray.png", "image/png", 0, stream, CancellationToken.None));
    }

    [Fact]
    public async Task AddAttachmentFileAsync_Should_Return_Null_For_Missing_Record()
    {
        var h = Build();
        using var stream = new MemoryStream(new byte[] { 1 });

        var result = await h.Service.AddAttachmentFileAsync(1, "xray.png", "image/png", 1, stream, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAttachmentFileAsync_Should_Save_File_And_Register_Attachment()
    {
        var h = Build(records: new[] { Record(1) });
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await h.Service.AddAttachmentFileAsync(1, "  xray.png", "image/png", 3, stream, CancellationToken.None);

        Assert.NotNull(result);
        h.FileStorage.Verify(f => f.SaveAsync("1", "xray.png", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        h.Attachments.Verify(a => a.AddAsync(
            It.Is<Attachment>(x => x.FileName == "xray.png" && x.FilePath == "1/xray.png" && x.MedicalRecordId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAttachmentAsync_Should_Return_Download_Info()
    {
        var attachment = new Attachment
        {
            Id = 3,
            MedicalRecordId = 1,
            FileName = "x.pdf",
            FilePath = "1/x.pdf",
            ContentType = "application/pdf",
            SizeBytes = 42
        };
        var h = Build(records: new[] { Record(1) }, attachments: new[] { attachment });

        var result = await h.Service.GetAttachmentAsync(1, 3, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("x.pdf", result!.FileName);
        Assert.Equal("1/x.pdf", result.PhysicalPath);
    }

    [Fact]
    public async Task GetAttachmentAsync_Should_Return_Null_When_Physical_File_Missing()
    {
        var attachment = new Attachment { Id = 3, MedicalRecordId = 1, FilePath = "gone.pdf" };
        var h = Build(attachments: new[] { attachment });
        h.FileStorage.Setup(f => f.GetPhysicalPath(It.IsAny<string?>())).Returns((string?)null);

        var result = await h.Service.GetAttachmentAsync(1, 3, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_Should_Remove_And_Delete_File()
    {
        var attachment = new Attachment { Id = 3, MedicalRecordId = 1, FilePath = "1/x.pdf" };
        var h = Build(attachments: new[] { attachment });

        var ok = await h.Service.DeleteAttachmentAsync(1, 3, CancellationToken.None);

        Assert.True(ok);
        h.FileStorage.Verify(f => f.Delete("1/x.pdf"), Times.Once);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_Should_Return_False_For_Missing()
    {
        var h = Build();

        Assert.False(await h.Service.DeleteAttachmentAsync(1, 3, CancellationToken.None));
    }
}
