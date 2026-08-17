using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Payment.Infrastructure;

/// <summary>
/// Xử lý thanh toán VNPay sandbox: dựng URL redirect và tạo/kiểm tra chữ ký.
/// Chữ ký HMAC-SHA256 theo chuẩn VNPay v2 — chuỗi "vnp_* ký" sắp xếp theo tên
/// tham số (không bao gồm vnp_SecureHash / vnp_SecureHashType), nối bằng '&'.
/// </summary>
public class VnPayService
{
    private readonly VnPayOptions _options;

    public VnPayService(IOptions<VnPayOptions> options) => _options = options.Value;

    /// <summary>Dựng URL thanh toán redirect tới cổng VNPay sandbox.</summary>
    public string BuildPaymentUrl(Guid orderId, decimal amount, string orderInfo, string returnUrl, string clientIp = "127.0.0.1")
    {
        var createDate = DateTime.UtcNow.AddHours(7); // VNPay dùng giờ VN (UTC+7)
        var amountVnd = (long)Math.Round(amount, MidpointRounding.AwayFromZero);

        var parameters = new Dictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _options.TmnCode,
            ["vnp_Locale"] = "vn",
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = orderId.ToString("N"),
            ["vnp_OrderInfo"] = Shorten(orderInfo, 250),
            ["vnp_OrderType"] = "other",
            ["vnp_Amount"] = (amountVnd * 100).ToString(), // VNPay yêu cầu nhân 100, không dấu phẩy
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_IpAddr"] = clientIp
        };

        var query = BuildQuery(parameters);
        return $"{_options.Url}?{query}&vnp_SecureHash={ComputeHash(query)}";
    }

    /// <summary>Kiểm tra chữ ký HMAC-SHA256 của bộ tham số vnp_* (nhận từ IPN/Return).</summary>
    public bool VerifySignature(IReadOnlyDictionary<string, string> vnpParameters, string? providedSecureHash)
    {
        if (string.IsNullOrEmpty(providedSecureHash)) return false;
        var raw = BuildQuery(vnpParameters);
        var expected = ComputeHash(raw);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(providedSecureHash));
    }

    /// <summary>
    /// Build chuỗi "key=value&amp;..." cho chữ ký — chỉ tính các tham số khác rỗng,
    /// bỏ qua vnp_SecureHash / vnp_SecureHashType, sắp xếp theo tên tham số.
    /// </summary>
    public static string BuildQuery(IReadOnlyDictionary<string, string> parameters)
    {
        var pairs = parameters
            .Where(kv => !string.IsNullOrEmpty(kv.Value)
                         && kv.Key is not ("vnp_SecureHash" or "vnp_SecureHashType"))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");
        return string.Join("&", pairs);
    }

    private string ComputeHash(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.HashSecret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Shorten(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s ?? string.Empty : s[..max];
}
