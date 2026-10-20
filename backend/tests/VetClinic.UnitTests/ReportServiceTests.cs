using MockQueryable.Moq;
using Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.UnitTests;

public class ReportServiceTests
{
    private static ReportService CreateService(
        IEnumerable<Invoice>? invoices = null,
        IEnumerable<Appointment>? appointments = null,
        IEnumerable<Pet>? pets = null,
        IEnumerable<Tutor>? tutors = null,
        IEnumerable<InventoryItem>? inventory = null,
        IEnumerable<Hospitalization>? hospitalizations = null,
        IEnumerable<VaccineApplication>? vaccines = null,
        IEnumerable<Reminder>? reminders = null)
    {
        static Mock<IRepository<T>> Repo<T>(IEnumerable<T>? items) where T : VetClinic.Domain.Common.BaseEntity
        {
            var mock = new Mock<IRepository<T>>();
            var data = items is null ? new List<T>() : items.ToList();
            mock.Setup(r => r.Query()).Returns(data.BuildMockDbSet().Object);
            return mock;
        }

        return new ReportService(
            Repo(invoices).Object,
            Repo(appointments).Object,
            Repo(pets).Object,
            Repo(tutors).Object,
            Repo(inventory).Object,
            Repo(hospitalizations).Object,
            Repo(vaccines).Object,
            Repo(reminders).Object);
    }

    [Fact]
    public async Task GetDashboardAsync_Should_Sum_Paid_Invoices_And_Compute_Delta()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var invoices = new List<Invoice>
        {
            new() { Status = InvoiceStatus.Pagada, IssueDate = from.AddDays(1), Total = 100m },
            new() { Status = InvoiceStatus.Pagada, IssueDate = from.AddDays(2), Total = 50m },
            new() { Status = InvoiceStatus.Emitida, IssueDate = from.AddDays(1), Total = 999m },
            new() { Status = InvoiceStatus.Pagada, IssueDate = from.AddDays(-10), Total = 100m }
        };

        var service = CreateService(
            invoices: invoices,
            pets: new[] { new Pet { Name = "Rocky" }, new Pet { Name = "Mia" } },
            tutors: new[] { new Tutor { FirstName = "Carlos" } },
            inventory: new[]
            {
                new InventoryItem { Name = "A", StockQuantity = 5, MinStock = 10, IsActive = true },
                new InventoryItem { Name = "B", StockQuantity = 20, MinStock = 10, IsActive = true }
            });

        var result = await service.GetDashboardAsync(from, to);

        Assert.Equal(150m, result.Revenue);
        Assert.Equal(2, result.InvoicesCount);
        Assert.Equal(100m, result.PreviousRevenue);
        Assert.Equal(50m, result.RevenueDeltaPct!.Value);
        Assert.Equal(2, result.PetsTotal);
        Assert.Equal(1, result.ClientsTotal);
        Assert.Equal(1, result.LowStockItems);
    }

    [Fact]
    public async Task GetDashboardAsync_Should_Return_Null_Delta_When_No_Previous_Period()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var service = CreateService(invoices: new[]
        {
            new Invoice { Status = InvoiceStatus.Pagada, IssueDate = from.AddDays(1), Total = 40m }
        });

        var result = await service.GetDashboardAsync(from, to);

        Assert.Null(result.RevenueDeltaPct);
        Assert.Equal(0m, result.PreviousRevenue);
    }

    [Fact]
    public async Task GetRevenuesAsync_Should_Group_Paid_Invoices_By_Month()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var service = CreateService(invoices: new[]
        {
            new Invoice { Status = InvoiceStatus.Pagada, IssueDate = new DateTime(2026, 1, 10), Total = 100m },
            new Invoice { Status = InvoiceStatus.Pagada, IssueDate = new DateTime(2026, 1, 20), Total = 50m },
            new Invoice { Status = InvoiceStatus.Pagada, IssueDate = new DateTime(2026, 2, 5), Total = 30m },
            new Invoice { Status = InvoiceStatus.Emitida, IssueDate = new DateTime(2026, 2, 6), Total = 999m }
        });

        var result = await service.GetRevenuesAsync(from, to);

        Assert.Equal(180m, result.Total);
        Assert.Equal(2, result.ByMonth.Count);
        Assert.Equal(150m, result.ByMonth[0].Total);
        Assert.Equal(1, result.ByMonth[0].Month);
        Assert.Equal(30m, result.ByMonth[1].Total);
        // El conteo alimenta el gráfico de barras de facturas por mes.
        Assert.Equal(2, result.ByMonth[0].InvoicesCount);
        Assert.Equal(1, result.ByMonth[1].InvoicesCount);
    }

    [Fact]
    public async Task GetOperationsAsync_Should_Compute_Operational_Kpis()
    {
        var today = DateTime.UtcNow.Date;

        var appointments = new List<Appointment>
        {
            new() { StartTime = today.AddHours(9), Status = AppointmentStatus.Programada, Urgency = UrgencyLevel.Alta },
            new() { StartTime = today.AddHours(11), Status = AppointmentStatus.EnConsulta, Urgency = UrgencyLevel.Baja },
            new() { StartTime = today.AddDays(1).AddHours(9), Status = AppointmentStatus.Programada, Urgency = UrgencyLevel.Media },
            new() { StartTime = today.AddDays(-3), Status = AppointmentStatus.Completada, Urgency = UrgencyLevel.Emergencia }
        };

        var hospitalizations = new List<Hospitalization>
        {
            new() { Status = HospitalizationStatus.Ingresado },
            new() { Status = HospitalizationStatus.EnTratamiento },
            new() { Status = HospitalizationStatus.Alta }
        };

        var vaccines = new List<VaccineApplication>
        {
            new() { NextDueDate = today.AddDays(10) },
            new() { NextDueDate = today.AddDays(60) },
            new() { NextDueDate = today.AddDays(-5) },
            new() { NextDueDate = null }
        };

        var reminders = new List<Reminder>
        {
            new() { IsSent = false },
            new() { IsSent = false },
            new() { IsSent = true }
        };

        var invoices = new List<Invoice>
        {
            new() { Status = InvoiceStatus.Emitida, Total = 100m },
            new() { Status = InvoiceStatus.Emitida, Total = 50m },
            new() { Status = InvoiceStatus.Pagada, Total = 999m }
        };

        var service = CreateService(
            appointments: appointments,
            hospitalizations: hospitalizations,
            vaccines: vaccines,
            reminders: reminders,
            invoices: invoices);

        var result = await service.GetOperationsAsync();

        Assert.Equal(2, result.AppointmentsToday);
        Assert.Equal(2, result.AppointmentsPending);
        Assert.Equal(1, result.AppointmentsInProgress);
        Assert.Equal(1, result.UrgentOpen);
        Assert.Equal(2, result.ActiveHospitalizations);
        Assert.Equal(1, result.UpcomingVaccines);
        Assert.Equal(2, result.PendingReminders);
        Assert.Equal(2, result.UnpaidInvoicesCount);
        Assert.Equal(150m, result.UnpaidInvoicesAmount);
    }

    [Fact]
    public async Task GetHospitalizationTrendAsync_Should_Agrupar_Por_Dia_Y_Completar_Dias_Vacios()
    {
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        var hospitalizations = new List<Hospitalization>
        {
            new() { StartDate = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc) },
            new() { StartDate = new DateTime(2026, 3, 1, 20, 0, 0, DateTimeKind.Utc) },
            new() { StartDate = new DateTime(2026, 3, 4, 10, 0, 0, DateTimeKind.Utc) },
            // Fuera del rango: no debe contar.
            new() { StartDate = new DateTime(2026, 2, 28, 10, 0, 0, DateTimeKind.Utc) }
        };

        var service = CreateService(hospitalizations: hospitalizations);

        var result = await service.GetHospitalizationTrendAsync(from, to);

        Assert.Equal(ClinicalGranularity.Dia, result.Granularity);
        Assert.Equal(3, result.Total);
        Assert.Equal(5, result.Points.Count);
        Assert.Equal(new[] { 2, 0, 0, 1, 0 }, result.Points.Select(p => p.Count));
        Assert.Equal("01/03", result.Points[0].Label);
        Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), result.Points[0].Start);
    }

    [Fact]
    public async Task GetHospitalizationTrendAsync_Should_Agrupar_Por_Semana_En_Rangos_Medios()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);

        var hospitalizations = new List<Hospitalization>
        {
            // Lunes 5 y lunes 12 de enero: semanas distintas.
            new() { StartDate = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc) },
            new() { StartDate = new DateTime(2026, 1, 7, 9, 0, 0, DateTimeKind.Utc) },
            new() { StartDate = new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc) }
        };

        var service = CreateService(hospitalizations: hospitalizations);

        var result = await service.GetHospitalizationTrendAsync(from, to);

        Assert.Equal(ClinicalGranularity.Semana, result.Granularity);
        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Points[0].Count);
        Assert.Equal(1, result.Points[1].Count);
        // Las semanas arrancan en lunes.
        Assert.Equal(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), result.Points[0].Start);
    }

    [Fact]
    public async Task GetHospitalizationTrendAsync_Should_Agrupar_Por_Mes_En_Rangos_Largos()
    {
        var from = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        var hospitalizations = new List<Hospitalization>
        {
            new() { StartDate = new DateTime(2025, 1, 20, 9, 0, 0, DateTimeKind.Utc) },
            new() { StartDate = new DateTime(2025, 1, 25, 9, 0, 0, DateTimeKind.Utc) },
            new() { StartDate = new DateTime(2026, 2, 3, 9, 0, 0, DateTimeKind.Utc) }
        };

        var service = CreateService(hospitalizations: hospitalizations);

        var result = await service.GetHospitalizationTrendAsync(from, to);

        Assert.Equal(ClinicalGranularity.Mes, result.Granularity);
        Assert.Equal(3, result.Total);
        // 14 meses, con los vacíos en cero.
        Assert.Equal(14, result.Points.Count);
        Assert.Equal(2, result.Points[0].Count);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Points[0].Start);
        Assert.Equal(1, result.Points[^1].Count);
    }

    [Fact]
    public async Task GetLowStockAsync_Should_Return_Only_Active_Items_Below_Minimum()
    {
        var service = CreateService(inventory: new[]
        {
            new InventoryItem { Id = 1, Name = "Amoxicilina", Category = "Antibióticos", Code = "MED-001", Unit = "tab", StockQuantity = 5, MinStock = 10, IsActive = true },
            new InventoryItem { Id = 2, Name = "Anestesia", Category = "Insumos", Code = "INS-001", Unit = "ml", StockQuantity = 200, MinStock = 40, IsActive = true },
            new InventoryItem { Id = 3, Name = "Inactivo", Category = "X", Code = "X-1", Unit = "u", StockQuantity = 0, MinStock = 5, IsActive = false }
        });

        var result = await service.GetLowStockAsync();

        var item = Assert.Single(result);
        Assert.Equal(1, item.Id);
        Assert.Equal("Amoxicilina", item.Name);
        Assert.Equal(5m, item.StockQuantity);
    }
}
