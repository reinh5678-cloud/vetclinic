using Microsoft.EntityFrameworkCore;
using VetClinic.Domain.Entities;
using VetClinic.Infrastructure.Data;

namespace VetClinic.UnitTests;

public class ConcurrencyModelTests
{
    private static VetClinicDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VetClinicDbContext>()
            .UseNpgsql("Host=localhost;Database=vetclinic;Username=vet;Password=vet")
            .Options;
        return new VetClinicDbContext(options);
    }

    [Theory]
    [InlineData(typeof(InventoryItem))]
    [InlineData(typeof(ProductLot))]
    [InlineData(typeof(Invoice))]
    public void Entity_Has_Xmin_Concurrency_Token(Type entityType)
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(entityType);
        Assert.NotNull(entity);

        var property = entity!.FindProperty("xmin");
        Assert.NotNull(property);
        Assert.True(property!.IsConcurrencyToken);
        Assert.Equal("xmin", property.GetColumnName());
    }
}
