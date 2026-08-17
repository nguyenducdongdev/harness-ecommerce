namespace Harness.Modules.Shipping.Application;

public class ShippingOptions
{
    public const string SectionName = "Shipping";
    public decimal BaseFeeNoiThan { get; set; } = 150_000;
    public decimal BaseFeeNgoaiThan { get; set; } = 350_000;
    public decimal BaseFeeTinh { get; set; } = 550_000;
    public int VolumetricDivisor { get; set; } = 6000;
    public decimal FreeShipOrderAmount { get; set; } = 5_000_000;
}
