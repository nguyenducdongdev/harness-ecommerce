using Harness.Modules.Order.Domain;
using Harness.Modules.Promotion.Domain;
using Harness.Modules.Loyalty.Domain;
using Xunit;

namespace Harness.UnitTests;

public class OrderTests
{
    private static Order NewOrder() => Order.Create(
        "Nguyễn Văn A", "0912345678", "a@test.vn",
        "123 Lê Lợi, Q1, TP.HCM", null,
        DeliveryMethod.Standard, PaymentMethod.Cod,
        new[] { (1, "SKU-1", "Sofa góc A", 5_000_000m, 2) });

    [Fact]
    public void Create_ComputesTotalsCorrectly()
    {
        var order = NewOrder();

        Assert.Equal(10_000_000m, order.ItemsTotal);
        Assert.Equal(10_000_000m, order.TotalAmount);
        Assert.Equal(OrderStatus.PendingConfirmation, order.Status);
        Assert.StartsWith("HD", order.OrderNumber);
    }

    [Fact]
    public void Create_WithoutPhone_Throws()
        => Assert.Throws<ArgumentException>(() => Order.Create(
            "Nguyễn Văn A", "", null, "Địa chỉ", null,
            DeliveryMethod.Standard, PaymentMethod.Cod,
            new[] { (1, "SKU-1", "Sofa góc A", 5_000_000m, 1) }));

    [Fact]
    public void Create_WithoutItems_Throws()
        => Assert.Throws<ArgumentException>(() => Order.Create(
            "Nguyễn Văn A", "0912345678", null, "Địa chỉ", null,
            DeliveryMethod.Standard, PaymentMethod.Cod,
            Array.Empty<(int, string, string, decimal, int)>()));

    [Fact]
    public void Transition_ValidFlow_Succeeds()
    {
        var order = NewOrder();
        order.TransitionTo(OrderStatus.Processing);
        order.TransitionTo(OrderStatus.Shipping);
        order.TransitionTo(OrderStatus.Delivered);
        order.TransitionTo(OrderStatus.Completed);

        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Transition_SkipStep_Throws()
    {
        var order = NewOrder();

        Assert.Throws<InvalidOperationException>(
            () => order.TransitionTo(OrderStatus.Delivered)); // Pending → Delivered không hợp lệ
    }
}

public class VoucherTests
{
    [Fact]
    public void CalculateDiscount_Percent_AppliesCap()
    {
        var voucher = Voucher.Create("SALE20", DiscountType.Percent, 20,
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1),
            minOrderAmount: 1_000_000, maxDiscountAmount: 2_000_000);

        var discount = voucher.CalculateDiscount(20_000_000, DateTimeOffset.UtcNow);

        Assert.Equal(2_000_000, discount); // 20% = 4tr nhưng bị cap 2tr
    }

    [Fact]
    public void CalculateDiscount_UnderMinOrder_Throws()
    {
        var voucher = Voucher.Create("SALE20", DiscountType.Percent, 20,
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1),
            minOrderAmount: 5_000_000);

        Assert.Throws<InvalidOperationException>(
            () => voucher.CalculateDiscount(1_000_000, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CalculateDiscount_Expired_Throws()
    {
        var voucher = Voucher.Create("OLD", DiscountType.FixedAmount, 100_000,
            DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));

        Assert.Throws<InvalidOperationException>(
            () => voucher.CalculateDiscount(1_000_000, DateTimeOffset.UtcNow));
    }
}

public class LoyaltyTests
{
    [Fact]
    public void EarnFromOrder_AccumulatesPointsAndTier()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid());

        account.EarnFromOrder(25_000_000); // 2500 điểm → Gold

        Assert.Equal(2500, account.Points);
        Assert.Equal(MemberTier.Gold, account.Tier);
    }

    [Fact]
    public void Redeem_MoreThanBalance_Throws()
    {
        var account = LoyaltyAccount.Open(Guid.NewGuid());
        account.EarnFromOrder(1_000_000); // 100 điểm

        Assert.Throws<InvalidOperationException>(() => account.Redeem(200));
    }
}
