using Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.UnitTests;

public class InvoiceServiceTests
{
    private static InvoiceService CreateService(
        ISettingsService settings,
        IRepository<InventoryItem>? inventory = null,
        Tutor? tutor = null)
    {
        var invoices = new Mock<IRepository<Invoice>>();
        inventory ??= new Mock<IRepository<InventoryItem>>().Object;
        var lots = new Mock<IRepository<ProductLot>>();
        var movements = new Mock<IRepository<StockMovement>>();
        var tutors = new Mock<IRepository<Tutor>>();
        var pets = new Mock<IRepository<Pet>>();
        var uow = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();

        var tx = new Mock<IUnitOfWorkTransaction>();
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tx.Object);
        invoices.Setup(r => r.AddAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice i, CancellationToken _) => i);
        invoices.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        if (tutor is not null)
            tutors.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(tutor);

        return new InvoiceService(invoices.Object, inventory, lots.Object, movements.Object, tutors.Object, pets.Object, uow.Object, settings, currentUser.Object);
    }

    private static ISettingsService Settings(decimal taxRate) => new FakeSettingsService
    {
        Billing = new BillingSettingsDto(taxRate, "MXN", "$", "FAC", new[] { "Efectivo", "Tarjeta", "Transferencia" }, string.Empty)
    };

    [Fact]
    public async Task CreateAsync_Should_Use_Default_16_Percent_When_Not_Configured()
    {
        var service = CreateService(Settings(0.16m), tutor: new Tutor { Id = 1, FirstName = "John", LastName = "Doe" });
        var request = new CreateInvoiceRequest(1, null, InvoiceStatus.Borrador, "Efectivo",
            new[] { new InvoiceLineRequest("Consulta", 1m, 100m, null) });

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(16m, result.Taxes);
        Assert.Equal(116m, result.Total);
    }

    [Fact]
    public async Task CreateAsync_Should_Use_Configured_TaxRate()
    {
        var service = CreateService(Settings(0m), tutor: new Tutor { Id = 1, FirstName = "John", LastName = "Doe" });
        var request = new CreateInvoiceRequest(1, null, InvoiceStatus.Borrador, "Efectivo",
            new[] { new InvoiceLineRequest("Consulta", 1m, 100m, null) });

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(0m, result.Taxes);
        Assert.Equal(100m, result.Total);
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Fractional_Quantity_For_Whole_Unit_Item()
    {
        var item = new InventoryItem { Id = 7, Name = "Jeringa", IsFractionable = false, IsActive = true, StockQuantity = 10m };
        var inventory = new Mock<IRepository<InventoryItem>>();
        inventory.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var service = CreateService(Settings(0.16m), inventory.Object, new Tutor { Id = 1, FirstName = "John", LastName = "Doe" });
        var request = new CreateInvoiceRequest(1, null, InvoiceStatus.Borrador, "Efectivo",
            new[] { new InvoiceLineRequest("Jeringa", 1.5m, 20m, 7) });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, CancellationToken.None));
    }
}
