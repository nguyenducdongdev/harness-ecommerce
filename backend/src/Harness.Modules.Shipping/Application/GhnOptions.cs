namespace Harness.Modules.Shipping.Application;

public class GhnOptions
{
    public const string SectionName = "Shipping:GHN";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn";
    public string ApiToken { get; set; } = default!;
    public int ShopId { get; set; }
    public int FromDistrictId { get; set; }
    public string FromWardCode { get; set; } = default!;
}
