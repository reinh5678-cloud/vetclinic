using Microsoft.EntityFrameworkCore;
using VetClinic.Domain;
using VetClinic.Domain.Entities;

namespace VetClinic.Infrastructure.Data;

public class VetClinicDbContext : DbContext
{
    public VetClinicDbContext(DbContextOptions<VetClinicDbContext> options) : base(options) { }

    public override int SaveChanges()
    {
        NormalizeDateTimeToUtc();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeToUtc();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void NormalizeDateTimeToUtc()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            foreach (var property in entry.Properties)
            {
                var clrType = property.Metadata.ClrType;
                if (clrType != typeof(DateTime) && clrType != typeof(DateTime?))
                    continue;

                if (property.CurrentValue is not DateTime value)
                    continue;

                var normalized = value.Kind switch
                {
                    DateTimeKind.Utc => value,
                    DateTimeKind.Local => value.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
                };

                if (normalized.Kind != value.Kind)
                    property.CurrentValue = normalized;
            }
        }
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Tutor> Tutors => Set<Tutor>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Hospitalization> Hospitalizations => Set<Hospitalization>();
    public DbSet<HospitalizationEvolution> HospitalizationEvolutions => Set<HospitalizationEvolution>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ProductLot> ProductLots => Set<ProductLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<VaccineApplication> VaccineApplications => Set<VaccineApplication>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.PinAccess).IsUnique();
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.Property(r => r.PermissionsJson).HasMaxLength(2000);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<Tutor>(e =>
        {
            e.Property(t => t.FirstName).HasMaxLength(60).IsRequired();
            e.Property(t => t.LastName).HasMaxLength(60).IsRequired();
            e.Property(t => t.Email).HasMaxLength(150);
            e.Property(t => t.Phone).HasMaxLength(30);
            e.Property(t => t.Address).HasMaxLength(200);
            e.Property(t => t.DocumentNumber).HasMaxLength(60);
            e.HasIndex(t => t.DocumentNumber).IsUnique();
            e.HasIndex(t => new { t.LastName, t.FirstName });
        });

        modelBuilder.Entity<Pet>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(60).IsRequired();
            e.Property(p => p.Breed).HasMaxLength(60);
            e.Property(p => p.Color).HasMaxLength(40);
            e.Property(p => p.WeightKg).HasPrecision(6, 2);
            e.Property(p => p.PhotoUrl).HasMaxLength(500);
            e.Property(p => p.IsSterilized).HasDefaultValue(false);
            e.HasOne(p => p.Tutor).WithMany(t => t.Pets).HasForeignKey(p => p.TutorId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => p.Name);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.Pet).WithMany(p => p.Appointments).HasForeignKey(a => a.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Vet).WithMany(u => u.Appointments).HasForeignKey(a => a.VetId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => a.StartTime);
        });

        modelBuilder.Entity<MedicalRecord>(e =>
        {
            e.Property(r => r.Symptoms).HasMaxLength(2000);
            e.Property(r => r.Findings).HasMaxLength(2000);
            e.Property(r => r.Diagnosis).HasMaxLength(1000);
            e.Property(r => r.TreatmentPlan).HasMaxLength(2000);
            e.Property(r => r.WeightKg).HasPrecision(6, 2);
            e.Property(r => r.TemperatureC).HasPrecision(4, 1);
            e.HasOne(r => r.Pet).WithMany(p => p.MedicalRecords).HasForeignKey(r => r.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Vet).WithMany().HasForeignKey(r => r.VetId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Appointment).WithOne(a => a.MedicalRecord).HasForeignKey<MedicalRecord>(r => r.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Prescription>(e =>
        {
            e.Property(p => p.MedicationName).HasMaxLength(120).IsRequired();
            e.HasOne(p => p.MedicalRecord).WithMany(r => r.Prescriptions).HasForeignKey(p => p.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabResult>(e =>
        {
            e.Property(l => l.TestType).HasMaxLength(100).IsRequired();
            e.Property(l => l.Result).HasMaxLength(1000);
            e.HasOne(l => l.MedicalRecord).WithMany(r => r.LabResults).HasForeignKey(l => l.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attachment>(e =>
        {
            e.Property(a => a.FileName).HasMaxLength(200).IsRequired();
            e.Property(a => a.FilePath).HasMaxLength(500).IsRequired();
            e.HasOne(a => a.MedicalRecord).WithMany(r => r.Attachments).HasForeignKey(a => a.MedicalRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Hospitalization>(e =>
        {
            e.HasOne(h => h.Pet).WithMany(p => p.Hospitalizations).HasForeignKey(h => h.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(h => h.AssignedVet).WithMany().HasForeignKey(h => h.AssignedVetId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HospitalizationEvolution>(e =>
        {
            e.Property(x => x.Temperature).HasPrecision(4, 1);
            e.HasOne(x => x.Hospitalization).WithMany(h => h.Evolutions).HasForeignKey(x => x.HospitalizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RecordedBy).WithMany().HasForeignKey(x => x.RecordedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicationAdministration>(e =>
        {
            e.Property(m => m.MedicationName).HasMaxLength(120).IsRequired();
            e.HasOne(m => m.Hospitalization).WithMany(h => h.MedicationAdministrations).HasForeignKey(m => m.HospitalizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.AdministeredBy).WithMany().HasForeignKey(m => m.AdministeredById).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.InventoryItem).WithMany().HasForeignKey(m => m.InventoryItemId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(m => m.InventoryItemId);
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.Property(i => i.Name).HasMaxLength(120).IsRequired();
            e.Property(i => i.Code).HasMaxLength(40).IsRequired();
            e.Property(i => i.Unit).HasMaxLength(20);
            e.Property(i => i.UnitPrice).HasPrecision(10, 2);
            e.Property(i => i.StockQuantity).HasPrecision(12, 3);
            e.Property(i => i.IsFractionable).HasDefaultValue(false);
            e.HasIndex(i => i.Code).IsUnique();
            e.Property<uint>("xmin").IsRowVersion();
        });

        modelBuilder.Entity<ProductLot>(e =>
        {
            e.Property(l => l.LotNumber).HasMaxLength(40).IsRequired();
            e.Property(l => l.AvailableQuantity).HasPrecision(12, 3);
            e.Property(l => l.IsActive).HasDefaultValue(true);
            e.HasOne(l => l.InventoryItem).WithMany(i => i.Lots).HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => l.InventoryItemId);
            e.HasIndex(l => l.ExpirationDate);
            e.Property<uint>("xmin").IsRowVersion();
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.Property(s => s.Quantity).HasPrecision(12, 3);
            e.Property(s => s.UnitCost).HasPrecision(10, 2);
            e.HasOne(s => s.InventoryItem).WithMany(i => i.Movements).HasForeignKey(s => s.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.ProductLot).WithMany().HasForeignKey(s => s.ProductLotId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.PerformedBy).WithMany().HasForeignKey(s => s.PerformedById).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
            e.Property(i => i.Currency).HasMaxLength(3).IsRequired().HasDefaultValue(InvoiceCurrencies.Default);
            e.Property(i => i.Subtotal).HasPrecision(12, 2);
            e.Property(i => i.Taxes).HasPrecision(12, 2);
            e.Property(i => i.Total).HasPrecision(12, 2);
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.HasOne(i => i.Tutor).WithMany(t => t.Invoices).HasForeignKey(i => i.TutorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Pet).WithMany().HasForeignKey(i => i.PetId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById).OnDelete(DeleteBehavior.Restrict);
            e.Property<uint>("xmin").IsRowVersion();
        });

        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.Property(l => l.Description).HasMaxLength(200).IsRequired();
            e.Property(l => l.Quantity).HasPrecision(12, 3);
            e.Property(l => l.UnitPrice).HasPrecision(10, 2);
            e.HasOne(l => l.Invoice).WithMany(i => i.Lines).HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.InventoryItem).WithMany().HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reminder>(e =>
        {
            e.Property(r => r.Recipient).HasMaxLength(150).IsRequired();
            e.Property(r => r.Message).HasMaxLength(1000);
            e.HasOne(r => r.Pet).WithMany().HasForeignKey(r => r.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => r.ScheduledAt);
        });

        modelBuilder.Entity<VaccineApplication>(e =>
        {
            e.HasOne(v => v.Pet).WithMany(p => p.VaccineApplications).HasForeignKey(v => v.PetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.InventoryItem).WithMany(i => i.VaccineApplications).HasForeignKey(v => v.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(v => v.PetId);
            e.HasIndex(v => v.NextDueDate);
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.Property(s => s.Section).HasMaxLength(60).IsRequired();
            e.Property(s => s.Json).IsRequired();
            e.HasIndex(s => s.Section).IsUnique();
        });
    }
}