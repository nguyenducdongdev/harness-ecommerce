using Harness.Modules.Promotion.Domain;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử flash sale: tạo, thêm sản phẩm, kiểm tra trạng thái active.</summary>
public class FlashSaleTests
{
    [Fact]
    public void Create_ValidInput_SetsProperties()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(2);
        var sale = FlashSale.Create("Flash Sale 8/8", start, end);

        Assert.Equal("Flash Sale 8/8", sale.Name);
        Assert.True(sale.IsActiveNow(start.AddMinutes(1)));
        Assert.False(sale.IsActiveNow(end.AddMinutes(1)));
    }

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var start = DateTimeOffset.UtcNow;
        Assert.Throws<ArgumentException>(() => FlashSale.Create("Bad", start, start.AddHours(-1)));
    }

    [Fact]
    public void AddItem_AccumulatesItems()
    {
        var sale = FlashSale.Create("Sale", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        sale.AddItem(1, 1_000_000, 10);

        Assert.Single(sale.Items);
    }

    [Fact]
    public void AddItem_DuplicateProduct_Throws()
    {
        var sale = FlashSale.Create("Sale", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        sale.AddItem(1, 1_000_000, 10);
        Assert.Throws<InvalidOperationException>(() => sale.AddItem(1, 900_000, 5));
    }

    [Fact]
    public void IncreaseSold_WithinLimit_UpdatesSold()
    {
        var sale = FlashSale.Create("Sale", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var item = sale.AddItem(1, 1_000_000, 10);
        item.IncreaseSold(3);

        Assert.Equal(3, item.QuantitySold);
        Assert.False(item.IsSoldOut);
    }

    [Fact]
    public void IncreaseSold_ExceedsLimit_Throws()
    {
        var sale = FlashSale.Create("Sale", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var item = sale.AddItem(1, 1_000_000, 2);
        Assert.Throws<InvalidOperationException>(() => item.IncreaseSold(3));
    }
}
