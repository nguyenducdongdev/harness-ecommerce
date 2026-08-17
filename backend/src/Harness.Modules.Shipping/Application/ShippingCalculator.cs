namespace Harness.Modules.Shipping.Application;

/// <summary>
/// Tính phí vận chuyển cho hàng nội thất cồng kềnh theo thể tích.
/// Công thức: volumetricWeight = (W × D × H) / 6000 (cm).
/// Phí = max(trọng lượng thực tế, volumetricWeight) × đơn giá theo khu vực.
/// </summary>
public class ShippingCalculator
{
    private readonly ShippingOptions _options;

    public ShippingCalculator(Microsoft.Extensions.Options.IOptions<ShippingOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Tính phí ship ước tính cho một biến thể sản phẩm.</summary>
    public ShippingQuote Calculate(int widthCm, int depthCm, int heightCm, double weightKg = 0, string zone = "noi-thanh")
    {
        var volumetricWeight = (widthCm * depthCm * heightCm) / (double)_options.VolumetricDivisor;
        var chargeableWeight = Math.Max(weightKg, volumetricWeight);

        var baseFee = zone switch
        {
            "ngoai-thanh" => _options.BaseFeeNgoaiThan,
            "tinh" => _options.BaseFeeTinh,
            _ => _options.BaseFeeNoiThan
        };

        // Phí theo cân tính (mỗi kg vượt 10kg cộng thêm)
        var extraKg = Math.Max(0, chargeableWeight - 10);
        var extraFee = extraKg * baseFee * 0.02m; // 2% baseFee mỗi kg vượt

        var totalFee = baseFee + extraFee;

        return new ShippingQuote(
            VolumetricWeight: Math.Round(volumetricWeight, 1),
            ChargeableWeight: Math.Round(chargeableWeight, 1),
            Zone: zone,
            ZoneLabel: zone switch
            {
                "noi-thanh" => "Nội thành",
                "ngoai-thanh" => "Ngoại thành",
                "tinh" => "Liên tỉnh",
                _ => "Nội thành"
            },
            EstimatedFee: Math.Round(totalFee),
            EstimatedDays: zone switch
            {
                "noi-thanh" => "1-2 ngày",
                "ngoai-thanh" => "2-4 ngày",
                "tinh" => "4-7 ngày",
                _ => "1-2 ngày"
            },
            FreeShipOrderAmount: _options.FreeShipOrderAmount
        );
    }
}

public record ShippingQuote(
    double VolumetricWeight,
    double ChargeableWeight,
    string Zone,
    string ZoneLabel,
    decimal EstimatedFee,
    string EstimatedDays,
    decimal FreeShipOrderAmount);
