namespace Harness.Modules.Payment.Infrastructure;

public class VnPayOptions
{
    public const string SectionName = "VnPay";
    public string Url { get; set; } = "https://sandbox.vnpay.vn/paymentv2/vpcpay.html";
    public string TmnCode { get; set; } = "HARNESS_TEST";
    public string HashSecret { get; set; } = "REPLACE_WITH_VNPAY_HASHSECRET";
    public string ReturnUrl { get; set; } = "http://localhost:3000/track";
}
