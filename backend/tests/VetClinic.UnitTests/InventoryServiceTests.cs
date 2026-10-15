using Moq;
using MockQueryable.Moq;
using VetClinic.Application.DTOs;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Domain.Common;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;

namespace VetClinic.UnitTests;

public class InventoryServiceTests
{
    private sealed record Harness(
        InventoryService Service,
        Mock<IRepository<InventoryItem>> Items,
        Mock<IRepository<ProductLot>> Lots,
        Mock<IRepository<StockMovement>> Movements,
        List<InventoryItem> ItemList,
        List<ProductLot> LotList,
        List<StockMovement> MovementList);

    private static Mock<IRepository<T>> Repo<T>(List<T> data) where T : BaseEntity
    {
        var repo = new Mock<IRepository<T>>();
        repo.Setup(r => r.Query()).Returns(() => data.BuildMockDbSet().Object);
        repo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => data.FirstOrDefault(e => e.Id == id));
        repo.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => { entity.Id = data.Count + 1; data.Add(entity); })
            .ReturnsAsync((T entity, CancellationToken _) => entity);
        repo.Setup(r => r.Update(It.IsAny<T>()));
        repo.Setup(r => r.Remove(It.IsAny<T>())).Callback<T>(entity => data.Remove(entity));
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return repo;
    }

    private static Harness Build(
        IEnumerable<InventoryItem>? items = null,
        IEnumerable<ProductLot>? lots = null,
        IEnumerable<StockMovement>? movements = null,
        int? userId = 5)
    {
        var itemList = (items ?? Array.Empty<InventoryItem>()).ToList();
        var lotList = (lots ?? Array.Empty<ProductLot>()).ToList();
        var movementList = (movements ?? Array.Empty<StockMovement>()).ToList();

        var itemRepo = Repo(itemList);
        var lotRepo = Repo(lotList);
        var movementRepo = Repo(movementList);

        var uow = new Mock<IUnitOfWork>();
        var tx = new Mock<IUnitOfWorkTransaction>();
        tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        tx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tx.Object);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(userId);

        var service = new InventoryService(itemRepo.Object, lotRepo.Object, movementRepo.Object, uow.Object, currentUser.Object);
        return new Harness(service, itemRepo, lotRepo, movementRepo, itemList, lotList, movementList);
    }

    private static InventoryItem Item(
        int id = 1, string name = "Alfa", string code = "A-1",
        decimal stock = 10, decimal minStock = 5, decimal price = 2m,
        bool active = true, bool fractionable = false) =>
        new()
        {
            Id = id, Name = name, Category = "Insumos", Code = code, Unit = "un",
            StockQuantity = stock, MinStock = minStock, UnitPrice = price,
            Supplier = "Prov", IsActive = active, IsFractionable = fractionable
        };

    private static ProductLot Lot(int id, int itemId, decimal qty, DateTime? expiry = null, bool active = true) =>
        new() { Id = id, InventoryItemId = itemId, LotNumber = $"L-{id}", AvailableQuantity = qty, ExpirationDate = expiry, IsActive = active };

    private static CreateInventoryItemRequest NewItem(
        string name = "Nuevo", string category = "Insumos", string code = "NEW-1", string unit = "un",
        decimal minStock = 5, decimal price = 3m, string supplier = "Prov") =>
        new(name, category, code, unit, minStock, price, supplier, false);

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_Should_Reject_Empty_Name()
    {
        var h = Build();
        await Assert.ThrowsAsync<ArgumentException>(() => h.Service.CreateAsync(NewItem(name: "   "), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Empty_Code()
    {
        var h = Build();
        await Assert.ThrowsAsync<ArgumentException>(() => h.Service.CreateAsync(NewItem(code: ""), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Negative_MinStock()
    {
        var h = Build();
        await Assert.ThrowsAsync<ArgumentException>(() => h.Service.CreateAsync(NewItem(minStock: -1), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Negative_UnitPrice()
    {
        var h = Build();
        await Assert.ThrowsAsync<ArgumentException>(() => h.Service.CreateAsync(NewItem(price: -0.5m), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_Code()
    {
        var h = Build(items: new[] { Item(code: "DUP-1") });
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Service.CreateAsync(NewItem(code: "DUP-1"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Should_Trim_And_Persist()
    {
        var h = Build();

        var result = await h.Service.CreateAsync(NewItem(name: "  Amoxicilina  ", code: "  MED-9  "), CancellationToken.None);

        Assert.Equal("Amoxicilina", result.Name);
        Assert.Equal("MED-9", result.Code);
        Assert.True(result.IsActive);
        Assert.Equal(0m, result.StockQuantity);
        Assert.Single(h.ItemList);
    }

    // ---------- ListAsync ----------

    [Fact]
    public async Task ListAsync_Should_Return_Items_Ordered_By_Name()
    {
        var h = Build(items: new[] { Item(2, "Zeta"), Item(1, "Alfa") });

        var result = await h.Service.ListAsync(null, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alfa", result[0].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Exclude_Inactive_By_Default()
    {
        var h = Build(items: new[] { Item(1, "Activo"), Item(2, "Inactivo", active: false) });

        var result = await h.Service.ListAsync(null, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Activo", result[0].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Include_Inactive_When_Requested()
    {
        var h = Build(items: new[] { Item(1, "Activo"), Item(2, "Inactivo", active: false) });

        var result = await h.Service.ListAsync(new InventoryQuery(IncludeInactive: true), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ListAsync_Should_Filter_By_Search()
    {
        var h = Build(items: new[] { Item(1, "Amoxicilina", code: "MED-1"), Item(2, "Venda", code: "INS-2") });

        var result = await h.Service.ListAsync(new InventoryQuery(Search: "amoxi"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Amoxicilina", result[0].Name);
    }

    [Fact]
    public async Task ListAsync_Should_Filter_LowOnly()
    {
        var h = Build(items: new[] { Item(1, "Bajo", stock: 3, minStock: 10), Item(2, "Ok", stock: 30, minStock: 10) });

        var result = await h.Service.ListAsync(new InventoryQuery(LowOnly: true), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Bajo", result[0].Name);
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_Should_Return_Null_For_Missing()
    {
        var h = Build();
        var result = await h.Service.UpdateAsync(99, new UpdateInventoryItemRequest("X", "C", "X", "un", 1, 1, "P", false), CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Fields()
    {
        var h = Build(items: new[] { Item(1) });

        var result = await h.Service.UpdateAsync(1, new UpdateInventoryItemRequest("  Nueva  ", "Farmacia", "NEW-1", "ml", 12, 9.5m, "  Prov  ", true), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Nueva", result!.Name);
        Assert.Equal("Farmacia", result.Category);
        Assert.Equal(12m, result.MinStock);
        Assert.Equal(9.5m, result.UnitPrice);
        Assert.True(result.IsFractionable);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_Duplicate_Code()
    {
        var h = Build(items: new[] { Item(1, "Uno", code: "A-1"), Item(2, "Dos", code: "B-2") });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.UpdateAsync(1, new UpdateInventoryItemRequest("Uno", "C", "B-2", "un", 1, 1, "P", false), CancellationToken.None));
    }

    // ---------- SetStatusAsync ----------

    [Fact]
    public async Task SetStatusAsync_Should_Toggle_Active()
    {
        var h = Build(items: new[] { Item(1, active: true) });

        var result = await h.Service.SetStatusAsync(1, new UpdateItemStatusRequest(false), CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
        Assert.False(h.ItemList.Single().IsActive);
    }

    [Fact]
    public async Task SetStatusAsync_Should_Return_Null_For_Missing()
    {
        var h = Build();
        Assert.Null(await h.Service.SetStatusAsync(99, new UpdateItemStatusRequest(false), CancellationToken.None));
    }

    // ---------- UpdateStockAsync ----------

    [Fact]
    public async Task UpdateStockAsync_Should_Reject_Zero()
    {
        var h = Build(items: new[] { Item(1) });
        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.UpdateStockAsync(1, new UpdateStockRequest(0, "x"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStockAsync_Should_Add_Stock_And_Create_Lot()
    {
        var h = Build(items: new[] { Item(1, stock: 10) });

        var result = await h.Service.UpdateStockAsync(1, new UpdateStockRequest(5, "Compra"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(15m, result!.StockQuantity);
        Assert.Single(h.LotList);
        Assert.Single(h.MovementList);
        Assert.Equal(StockMovementType.Ajuste, h.MovementList[0].Type);
    }

    [Fact]
    public async Task UpdateStockAsync_Should_Consume_Fefo_By_Expiry()
    {
        var today = DateTime.UtcNow.Date;
        var h = Build(
            items: new[] { Item(1, stock: 10) },
            lots: new[]
            {
                Lot(1, 1, 4, today.AddDays(60)),
                Lot(2, 1, 6, today.AddDays(5)),
                Lot(3, 1, 10, today.AddDays(120))
            });

        var result = await h.Service.UpdateStockAsync(1, new UpdateStockRequest(-7, "Salida"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3m, result!.StockQuantity);
        Assert.Equal(0m, h.LotList.Single(l => l.Id == 2).AvailableQuantity);
        Assert.Equal(3m, h.LotList.Single(l => l.Id == 1).AvailableQuantity);
        Assert.Equal(10m, h.LotList.Single(l => l.Id == 3).AvailableQuantity);
    }

    [Fact]
    public async Task UpdateStockAsync_Should_Reject_Insufficient()
    {
        var h = Build(items: new[] { Item(1, stock: 2) }, lots: new[] { Lot(1, 1, 2) });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Service.UpdateStockAsync(1, new UpdateStockRequest(-5, "Salida"), CancellationToken.None));
    }

    // ---------- AddLotAsync ----------

    [Fact]
    public async Task AddLotAsync_Should_Increase_Stock_And_Register_Movement()
    {
        var h = Build(items: new[] { Item(1, stock: 5) });

        var result = await h.Service.AddLotAsync(1, new AddLotRequest("  L-77  ", DateTime.UtcNow.AddMonths(6), 20, "Compra"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("L-77", result!.LotNumber);
        Assert.Equal(25m, h.ItemList.Single().StockQuantity);
        Assert.Equal(StockMovementType.EntradaCompra, h.MovementList.Single().Type);
    }

    [Fact]
    public async Task AddLotAsync_Should_Reject_NonPositive_Quantity()
    {
        var h = Build(items: new[] { Item(1) });
        await Assert.ThrowsAsync<ArgumentException>(() =>
            h.Service.AddLotAsync(1, new AddLotRequest("L", null, 0, ""), CancellationToken.None));
    }

    [Fact]
    public async Task AddLotAsync_Should_Return_Null_For_Missing_Item()
    {
        var h = Build();
        Assert.Null(await h.Service.AddLotAsync(99, new AddLotRequest("L", null, 1, ""), CancellationToken.None));
    }

    // ---------- Lot deactivation / reactivation ----------

    [Fact]
    public async Task DeactivateLotAsync_Should_Reduce_Stock()
    {
        var h = Build(items: new[] { Item(1, stock: 10) }, lots: new[] { Lot(1, 1, 6) });

        var ok = await h.Service.DeactivateLotAsync(1, CancellationToken.None);

        Assert.True(ok);
        Assert.False(h.LotList.Single().IsActive);
        Assert.Equal(4m, h.ItemList.Single().StockQuantity);
        Assert.Single(h.MovementList);
    }

    [Fact]
    public async Task DeactivateLotAsync_Should_Return_False_When_Already_Inactive()
    {
        var h = Build(items: new[] { Item(1) }, lots: new[] { Lot(1, 1, 6, active: false) });
        Assert.False(await h.Service.DeactivateLotAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task ReactivateLotAsync_Should_Restore_Stock()
    {
        var h = Build(items: new[] { Item(1, stock: 4) }, lots: new[] { Lot(1, 1, 6, active: false) });

        var ok = await h.Service.ReactivateLotAsync(1, CancellationToken.None);

        Assert.True(ok);
        Assert.True(h.LotList.Single().IsActive);
        Assert.Equal(10m, h.ItemList.Single().StockQuantity);
    }

    [Fact]
    public async Task ReactivateLotAsync_Should_Return_False_When_Already_Active()
    {
        var h = Build(items: new[] { Item(1) }, lots: new[] { Lot(1, 1, 6) });
        Assert.False(await h.Service.ReactivateLotAsync(1, CancellationToken.None));
    }

    // ---------- ListLotsAsync ----------

    [Fact]
    public async Task ListLotsAsync_Should_Hide_Inactive_By_Default()
    {
        var h = Build(lots: new[] { Lot(1, 1, 6), Lot(2, 1, 3, active: false) });

        var result = await h.Service.ListLotsAsync(1, false, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task ListLotsAsync_Should_Include_Inactive_When_Requested()
    {
        var h = Build(lots: new[] { Lot(1, 1, 6), Lot(2, 1, 3, active: false) });

        var result = await h.Service.ListLotsAsync(1, true, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    // ---------- ListExpiringLotsAsync ----------

    [Fact]
    public async Task ListExpiringLotsAsync_Should_Return_Soon_And_Expired()
    {
        var today = DateTime.UtcNow.Date;
        var item = Item(1, "Vacuna");
        var h = Build(
            items: new[] { item },
            lots: new[]
            {
                Lot(1, 1, 5, today.AddDays(10)),
                Lot(2, 1, 5, today.AddDays(-2)),
                Lot(3, 1, 5, today.AddDays(200)),
                Lot(4, 1, 0, today.AddDays(3))
            });
        foreach (var l in h.LotList) l.InventoryItem = item;

        var result = await h.Service.ListExpiringLotsAsync(30, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.True(result.Single(r => r.LotNumber == "L-2").IsExpired);
        Assert.Equal("Vacuna", result[0].ItemName);
    }

    [Fact]
    public async Task ListExpiringLotsAsync_Should_Ignore_Lots_Without_Expiry()
    {
        var h = Build(lots: new[] { Lot(1, 1, 5) });

        var result = await h.Service.ListExpiringLotsAsync(30, CancellationToken.None);

        Assert.Empty(result);
    }
}
