using VetClinic.Domain.Enums;

namespace VetClinic.Application.DTOs;

public record CreateTutorRequest(string FirstName, string LastName, string Email, string Phone, string Address, string? DocumentNumber);

public record UpdateTutorRequest(string FirstName, string LastName, string Email, string Phone, string Address, string? DocumentNumber);

public record TutorDto(int Id, string FirstName, string LastName, string Email, string Phone, string Address, string? DocumentNumber);

public record TutorListDto(int Id, string FirstName, string LastName, string Email, string Phone, string Address, string DocumentNumber, int PetsCount);

public record CreatePetRequest(string Name, Species Species, string Breed, DateTime? BirthDate, Sex Sex, decimal WeightKg, string Color, int? TutorId, bool IsSterilized, string? PhotoUrl);

public record UpdatePetRequest(string Name, Species Species, string Breed, DateTime? BirthDate, Sex Sex, decimal WeightKg, string Color, int? TutorId, bool IsSterilized, string? PhotoUrl);

public record SetPetActiveRequest(bool Active);

public record PetDto(int Id, string Name, string Species, string Breed, DateTime? BirthDate, string Sex, decimal WeightKg, string Color, int? TutorId, int? AgeYears, bool IsSterilized, string? PhotoUrl, string? TutorName, bool IsActive);
