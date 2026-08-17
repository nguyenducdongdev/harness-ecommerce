using Harness.Modules.Payment.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử dịch vụ VNPay sandbox: dựng URL + kiểm tra chữ ký HMAC-SHA256.</summary>
public class VnPayServiceTests
{
    private static VnPayService NewService() => new(Options.Create(new VnPayOptions
    {
        Url = "https://sandbox.vnpay.vn/paymentv2/vpcpay.html",
        TmnCode = "HARNESS_TEST",
        HashSecret = "test-secret",
        ReturnUrl = "http://localhost:3000/track"
    }));

    [Fact]
    public void BuildPaymentUrl_HostsVnPayAndSignedHash()
    {
        var url = NewService().BuildPaymentUrl(
            Guid.Parse("11111111-1111-1111-1111-111111111111"), 500_000m,
            "Thanh toan don hang", "http://localhost:3000/track");

        Assert.Contains("paymentv2/vpcpay.html", url);
        Assert.Contains("vnp_SecureHash=", url);
        Assert.Contains("vnp_Amount=50000000", url); // vnd * 100, không lẻ
        Assert.Contains("vnp_TmnCode=HARNESS_TEST", url);
    }

    [Fact]
    public void VerifySignature_AcceptsValidlySignedParams()
    {
        var service = NewService();
        var url = service.BuildPaymentUrl(Guid.NewGuid(), 120_000m, "x", "http://localhost:3000/track");
        var vnp = ParseQuery(url.Split('?')[1]);
        var hash = vnp["vnp_SecureHash"];
        vnp.Remove("vnp_SecureHash");

        Assert.True(service.VerifySignature(vnp, hash));
    }

    [Fact]
    public void VerifySignature_RejectsTamperedAmount()
    {
        var service = NewService();
        var url = service.BuildPaymentUrl(Guid.NewGuid(), 120_000m, "x", "http://localhost:3000/track");
        var vnp = ParseQuery(url.Split('?')[1]);
        var hash = vnp["vnp_SecureHash"];
        vnp.Remove("vnp_SecureHash");
        vnp["vnp_Amount"] = "9999999999"; // giả mạo số tiền

        Assert.False(service.VerifySignature(vnp, hash));
    }

    [Fact]
    public void VerifySignature_MissingHash_ReturnsFalse()
    {
        var service = NewService();
        var vnp = new Dictionary<string, string> { ["vnp_Amount"] = "12000000" };
        Assert.False(service.VerifySignature(vnp, null));
    }

    private static Dictionary<string, string> ParseQuery(string query)
        => query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => parts[1]);
}
