using Harness.Modules.Inventory.Domain;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử tồn kho theo showroom: giữ chờ / hoàn giữ chờ / điều chỉnh.</summary>
public class StockLevelTests
{
    [Fact]
    public void Create_NegativeInitial_Throws()
        => Assert.Throws<ArgumentException>(() => StockLevel.Create(1, "SKU-A", -1));

    [Fact]
    public void Reserve_MovesAvailableToReserved()
    {
        var stock = StockLevel.Create(1, "SKU-A", 10);
        stock.Reserve(3);

        Assert.Equal(7, stock.QuantityAvailable);
        Assert.Equal(3, stock.QuantityReserved);
    }

    [Fact]
    public void Reserve_InsufficientStock_Throws()
    {
        var stock = StockLevel.Create(1, "SKU-A", 2);
        Assert.Throws<InvalidOperationException>(() => stock.Reserve(5));
    }

    [Fact]
    public void Release_reservation_ReturnsAvailable()
    {
        var stock = StockLevel.Create(1, "SKU-A", 10);
        stock.Reserve(4);
        stock.ReleaseReservation(4);

        Assert.Equal(10, stock.QuantityAvailable);
        Assert.Equal(0, stock.QuantityReserved);
    }

    [Fact]
    public void Release_MoreThanReserved_Throws()
    {
        var stock = StockLevel.Create(1, "SKU-A", 10);
        stock.Reserve(2);
        Assert.Throws<InvalidOperationException>(() => stock.ReleaseReservation(5));
    }
}
