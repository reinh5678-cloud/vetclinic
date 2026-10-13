using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VetClinic.Domain;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.Infrastructure.Data;

public static class SeedData
{
    private const string AdminPin = "1111";
    private const string VetPin = "2222";
    private const string ReceptionPin = "3333";

    private static readonly string[] AdminPermissions = Permissions.All;

    private static readonly string[] VetPermissions =
    {
        Permissions.PatientsView, Permissions.PatientsManage,
        Permissions.AppointmentsView, Permissions.AppointmentsManage,
        Permissions.MedicalView, Permissions.MedicalManage,
        Permissions.HospitalizationView, Permissions.HospitalizationManage,
        Permissions.InventoryView, Permissions.InventoryManage,
        Permissions.InvoicesView, Permissions.InvoicesManage
    };

    private static readonly string[] ReceptionPermissions =
    {
        Permissions.PatientsView, Permissions.PatientsManage,
        Permissions.AppointmentsView, Permissions.AppointmentsManage,
        Permissions.InvoicesView, Permissions.InvoicesManage
    };

    public static async Task SeedAsync(VetClinicDbContext db)
    {
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Name = "Administrador", PermissionsJson = JsonSerializer.Serialize(AdminPermissions) },
                new Role { Name = "Veterinario", PermissionsJson = JsonSerializer.Serialize(VetPermissions) },
                new Role { Name = "Recepcionista", PermissionsJson = JsonSerializer.Serialize(ReceptionPermissions) }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var adminRole = await db.Roles.SingleAsync(r => r.Name == "Administrador");
            var vetRole = await db.Roles.SingleAsync(r => r.Name == "Veterinario");
            var receptionRole = await db.Roles.SingleAsync(r => r.Name == "Recepcionista");

            var admin = new User { Username = "admin", FullName = "Administrador", Email = "admin@vetclinic.local", RoleId = adminRole.Id, PinAccess = AdminPin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!") };
            var reception = new User { Username = "recepcion", FullName = "Recepcionista", Email = "recepcion@vetclinic.local", RoleId = receptionRole.Id, PinAccess = ReceptionPin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recep123!") };
            var vet = new User { Username = "vet", FullName = "Dra. Maria Veterinaria", Email = "vet@vetclinic.local", RoleId = vetRole.Id, PinAccess = VetPin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vet123!") };
            db.Users.AddRange(admin, reception, vet);

            var tutor = new Tutor { FirstName = "Carlos", LastName = "Perez", Email = "carlos@example.com", Phone = "555-0100", Address = "Av. Principal 123", DocumentNumber = "12345678A" };
            var tutor2 = new Tutor { FirstName = "Ana", LastName = "Lopez", Email = "ana@example.com", Phone = "555-0200", Address = "Calle Sol 45", DocumentNumber = "87654321B" };
            db.Tutors.AddRange(tutor, tutor2);

            var rocky = new Pet { Name = "Rocky", Species = Species.Perro, Breed = "Labrador", BirthDate = DateTime.UtcNow.AddYears(-3), Sex = Sex.Macho, WeightKg = 28.5m, Color = "Dorado", Tutor = tutor, IsSterilized = false };
            var mia = new Pet { Name = "Mia", Species = Species.Gato, Breed = "Siames", BirthDate = DateTime.UtcNow.AddYears(-2), Sex = Sex.Hembra, WeightKg = 4.2m, Color = "Gris", Tutor = tutor2, IsSterilized = true };
            db.Pets.AddRange(rocky, mia);

            var vaccine = new InventoryItem { Name = "Vacuna Antirrábica", Category = "Vacunas", Code = "VAC-001", Unit = "dosis", StockQuantity = 50, MinStock = 10, UnitPrice = 25m, Supplier = "BioVet" };
            var antibiotic = new InventoryItem { Name = "Amoxicilina 250mg", Category = "Antibióticos", Code = "MED-001", Unit = "tab", StockQuantity = 8, MinStock = 20, UnitPrice = 5.5m, Supplier = "FarmaVet" };
            var anesthesia = new InventoryItem { Name = "Anestesia Isofluorano", Category = "Insumos", Code = "INS-001", Unit = "ml", StockQuantity = 200, MinStock = 40, UnitPrice = 3m, Supplier = "AnesMed" };
            db.InventoryItems.AddRange(vaccine, antibiotic, anesthesia);

            db.Appointments.Add(new Appointment
            {
                StartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
                DurationMinutes = 30,
                Urgency = UrgencyLevel.Media,
                Reason = "Vacunación anual",
                Status = AppointmentStatus.Programada,
                Pet = rocky,
                Vet = vet
            });

            await db.SaveChangesAsync();

            foreach (var item in new[] { vaccine, antibiotic, anesthesia })
            {
                if (item.StockQuantity > 0)
                {
                    db.ProductLots.Add(new ProductLot
                    {
                        InventoryItem = item,
                        LotNumber = "INICIAL",
                        AvailableQuantity = item.StockQuantity
                    });
                }
            }

            var rockyId = db.Pets.First(p => p.Name == "Rocky").Id;
            db.MedicalRecords.Add(new MedicalRecord
            {
                PetId = rockyId,
                Vet = vet,
                Symptoms = "Fiebre y pérdida de apetito",
                Findings = "Deshidratación leve, enrojecimiento de encías",
                Diagnosis = "Gingivitis leve",
                TreatmentPlan = "Limpieza dental + amoxicilina por 7 días",
                WeightKg = 28.5m,
                TemperatureC = 39.4m
            });

            var vaccineNextDue = DateTime.UtcNow.AddMonths(1).Date;
            db.VaccineApplications.Add(new VaccineApplication
            {
                PetId = rockyId,
                InventoryItemId = vaccine.Id,
                ApplicationDate = DateTime.UtcNow.AddMonths(-11),
                NextDueDate = vaccineNextDue
            });
            db.Reminders.Add(new Reminder
            {
                PetId = rockyId,
                Recipient = "carlos@example.com",
                Channel = "Correo",
                Message = $"Vacuna {vaccine.Name} aplicada — próxima dosis {vaccineNextDue:dd/MM/yyyy}",
                ScheduledAt = vaccineNextDue,
                IsSent = false
            });

            await db.SaveChangesAsync();
        }
        else
        {
            await EnsureDemoPinsAsync(db);
        }
    }

    private static async Task EnsureDemoPinsAsync(VetClinicDbContext db)
    {
        var pins = new Dictionary<string, string>
        {
            ["admin"] = AdminPin,
            ["vet"] = VetPin,
            ["recepcion"] = ReceptionPin
        };

        foreach (var username in pins.Keys)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);
            if (user is not null && string.IsNullOrEmpty(user.PinAccess))
            {
                user.PinAccess = pins[username];
            }
        }

        await db.SaveChangesAsync();
    }
}