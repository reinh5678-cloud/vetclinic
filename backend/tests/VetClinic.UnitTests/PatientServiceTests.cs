using Moq;
using MockQueryable.Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.UnitTests;

public class PatientServiceTests
{
    private static (PatientService Service, Mock<IRepository<Tutor>> Tutors, Mock<IRepository<Pet>> Pets) Build()
    {
        var tutorRepo = new Mock<IRepository<Tutor>>();
        var petRepo = new Mock<IRepository<Pet>>();
        return (new PatientService(tutorRepo.Object, petRepo.Object), tutorRepo, petRepo);
    }

    [Fact]
    public async Task CreateTutorAsync_Should_Create_Tutor()
    {
        var (service, tutorRepo, _) = Build();
        var request = new CreateTutorRequest("John", "Doe", "john@test.com", "1234567890", "123 Main St", "12345678");
        tutorRepo.Setup(r => r.AddAsync(It.IsAny<Tutor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tutor t, CancellationToken ct) => { t.Id = 1; return t; });
        tutorRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await service.CreateTutorAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task CreateTutorAsync_Should_Reject_Missing_FirstName()
    {
        var (service, _, _) = Build();
        var request = new CreateTutorRequest("  ", "Doe", "john@test.com", "123", "Main St", null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTutorAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePetAsync_Should_Create_Pet()
    {
        var (service, tutorRepo, petRepo) = Build();
        var tutors = new List<Tutor> { new() { Id = 1, FirstName = "John", LastName = "Doe" } };
        tutorRepo.Setup(r => r.Query()).Returns(tutors.BuildMockDbSet().Object);
        var request = new CreatePetRequest("Fido", Species.Perro, "Labrador", DateTime.UtcNow.AddYears(-2), Sex.Macho, 15.5m, "Brown", 1, true, null);
        petRepo.Setup(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pet p, CancellationToken ct) => { p.Id = 1; return p; });
        petRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await service.CreatePetAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Fido", result.Name);
    }

    [Fact]
    public async Task CreatePetAsync_Should_Reject_Unknown_Tutor()
    {
        var (service, tutorRepo, _) = Build();
        tutorRepo.Setup(r => r.Query()).Returns(new List<Tutor>().BuildMockDbSet().Object);
        var request = new CreatePetRequest("Fido", Species.Perro, "Labrador", null, Sex.Macho, 10m, "Brown", 99, false, null);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePetAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ListPetsAsync_Should_Return_Pets()
    {
        var (service, _, petRepo) = Build();
        var pets = new List<Pet> { new() { Id = 1, Name = "Fido", Breed = "Labrador", Species = Species.Perro, Tutor = new Tutor { FirstName = "John", LastName = "Doe" } } };
        petRepo.Setup(r => r.Query()).Returns(pets.BuildMockDbSet().Object);

        var result = await service.ListPetsAsync(ct: CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task ListPetsAsync_Should_Filter_By_Search_And_Species()
    {
        var (service, _, petRepo) = Build();
        var pets = new List<Pet>
        {
            new() { Id = 1, Name = "Fido", Breed = "Labrador", Species = Species.Perro, IsActive = true },
            new() { Id = 2, Name = "Michi", Breed = "Siames", Species = Species.Gato, IsActive = true }
        };
        petRepo.Setup(r => r.Query()).Returns(pets.BuildMockDbSet().Object);

        var byName = await service.ListPetsAsync("fid", null, null, CancellationToken.None);
        var bySpecies = await service.ListPetsAsync(null, Species.Gato, null, CancellationToken.None);

        Assert.Single(byName);
        Assert.Equal("Fido", byName[0].Name);
        Assert.Single(bySpecies);
        Assert.Equal("Michi", bySpecies[0].Name);
    }

    [Fact]
    public async Task SetPetActiveAsync_Should_Toggle_Status()
    {
        var (service, _, petRepo) = Build();
        var pets = new List<Pet> { new() { Id = 1, Name = "Fido", Breed = "", Species = Species.Perro, IsActive = true } };
        petRepo.Setup(r => r.Query()).Returns(pets.BuildMockDbSet().Object);
        petRepo.Setup(r => r.Update(It.IsAny<Pet>()));
        petRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await service.SetPetActiveAsync(1, false, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    [Fact]
    public async Task UpdateTutorAsync_Should_Return_Null_When_Not_Found()
    {
        var (service, tutorRepo, _) = Build();
        tutorRepo.Setup(r => r.Query()).Returns(new List<Tutor>().BuildMockDbSet().Object);

        var result = await service.UpdateTutorAsync(42, new UpdateTutorRequest("John", "Doe", "j@t.com", "1", "St", null), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePetAsync_Should_Return_Null_When_Not_Found()
    {
        var (service, _, petRepo) = Build();
        petRepo.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        var result = await service.UpdatePetAsync(42, new UpdatePetRequest("Fido", Species.Perro, "L", null, Sex.Macho, 1m, "B", null, false, null), CancellationToken.None);

        Assert.Null(result);
    }
}
