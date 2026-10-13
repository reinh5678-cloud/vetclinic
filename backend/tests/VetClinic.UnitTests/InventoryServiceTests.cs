using Moq;
using MockQueryable.Moq;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Entities;

namespace VetClinic.UnitTests;

public class InventoryServiceTests
{
    [Fact]
    public async Task ListAsync_Should_Return_Items()
    {
        var items = new Mock<IRepository<InventoryItem>>();
        var lots = new Mock<IRepository<ProductLot>>();
        var movements = new Mock<IRepository<StockMovement>>();
        var uow = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUser>();
        var service = new InventoryService(items.Object, lots.Object, movements.Object, uow.Object, currentUser.Object);
        items.Setup(r => r.Query()).Returns(new[]
        {
            new InventoryItem { Id = 2, Name = "Zeta", StockQuantity = 5 },
            new InventoryItem { Id = 1, Name = "Alfa", StockQuantity = 10 }
        }.BuildMockDbSet().Object);

        var result = await service.ListAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Alfa", result[0].Name);
    }
}
