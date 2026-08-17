using Harness.Modules.Shipping.Application;
using Microsoft.Extensions.Options;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử phí vận chuyển theo thể tích (W×D×H / 6000) cho hàng nội thất.</summary>
public class ShippingCalculatorTests
{
    private static ShippingCalculator NewCalculator() =>
        new(Options.Create(new ShippingOptions
        {
            BaseFeeNoiThan = 150_000,
            BaseFeeNgoaiThan = 350_000,
            BaseFeeTinh = 550_000,
            VolumetricDivisor = 6000,
            FreeShipOrderAmount = 5_000_000
        }));

    [Fact]
    public void Calculate_VolumetricWeight_ForSofaAngle()
    {
        // 280 × 190 × 82 cm → 280*190*82 / 6000 = 727.1 kg
        var quote = NewCalculator().Calculate(280, 190, 82);

        Assert.Equal(727.1, quote.VolumetricWeight, precision: 1);
        Assert.Equal(727.1, quote.ChargeableWeight, precision: 1);
        Assert.Equal("noi-thanh", quote.Zone);
    }

    [Fact]
    public void Calculate_UsesActualWeight_WhenHeavierThanVolumetric()
    {
        // volumetric nhỏ, trọng lượng thực 40kg chiếm ưu thế
        var quote = NewCalculator().Calculate(45, 52, 88, weightKg: 40);

        // volumetric = 45*52*88/6000 = 34.3 → chargeable = 40
        Assert.Equal(40, quote.ChargeableWeight, precision: 1);
    }

    [Fact]
    public void Calculate_Fee_DependsOnZone()
    {
        var noiThanh = NewCalculator().Calculate(120, 60, 75, weightKg: 5);
        var tinh = NewCalculator().Calculate(120, 60, 75, weightKg: 5, zone: "tinh");

        Assert.True(tinh.EstimatedFee > noiThanh.EstimatedFee);
        Assert.Equal("Liên tỉnh", tinh.ZoneLabel);
    }

    [Fact]
    public void Calculate_ExtraKg_SurchargeApplied()
    {
        // chargeable 40kg → 30kg vượt 10kg → +2% baseFee/kg
        var quote = NewCalculator().Calculate(45, 52, 88, weightKg: 40);

        var expectedExtra = 30 * 150_000m * 0.02m; // 90.000 VND
        Assert.Equal(150_000 + expectedExtra, quote.EstimatedFee);
    }

    [Fact]
    public void Calculate_RoundedVolumetricWeight()
        => Assert.Equal(727.1, NewCalculator().Calculate(280, 190, 82, weightKg: 0).VolumetricWeight, precision: 1);
}
