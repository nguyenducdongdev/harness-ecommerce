using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Harness.Modules.Shipping.Application;
using Harness.Modules.Shipping.Application.Providers;
using Harness.Modules.Shipping.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Shipping.Infrastructure.Providers;

public sealed class GhnShippingProvider : IShippingProvider
{
    private readonly HttpClient _httpClient;
    private readonly GhnOptions _options;
    private readonly ILogger<GhnShippingProvider> _logger;

    public Carrier Carrier => Carrier.Ghn;

    public GhnShippingProvider(HttpClient httpClient, IOptions<GhnOptions> options, ILogger<GhnShippingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ShippingFeeResult> CalculateAsync(ShippingFeeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new ShippingFeeResult(Carrier.Ghn, 0, "", false, "GHN chưa được bật trong cấu hình.");

        var (success, raw) = await ExecuteWithRetryAsync(request, cancellationToken);
        if (!success || string.IsNullOrWhiteSpace(raw))
            return new ShippingFeeResult(Carrier.Ghn, 0, "", false, "Không nhận được phản hồi hợp lệ từ GHN.", raw);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var code = root.GetProperty("code").GetInt32();
            if (code != 200)
            {
                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : null;
                return new ShippingFeeResult(Carrier.Ghn, 0, "", false, message ?? "Lỗi từ GHN.", raw);
            }

            var total = root.GetProperty("data").GetProperty("total").GetInt32();
            return new ShippingFeeResult(Carrier.Ghn, total, "2-4 ngày", true, "OK", raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi parse phản hồi GHN.");
            return new ShippingFeeResult(Carrier.Ghn, 0, "", false, "Không thể parse phản hồi GHN.", raw);
        }
    }

    private async Task<(bool Success, string? Body)> ExecuteWithRetryAsync(ShippingFeeRequest request, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var payload = new
                {
                    from_district_id = _options.FromDistrictId,
                    from_ward_code = _options.FromWardCode,
                    to_district_id = request.ToDistrictId,
                    to_ward_code = request.ToWardCode,
                    weight = request.WeightKg * 1000,
                    length = request.LengthCm,
                    width = request.WidthCm,
                    height = request.HeightCm,
                    insurance_value = request.InsuranceValue,
                    coupon = (string?)null
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/shipping-order/fee")
                {
                    Content = JsonContent.Create(payload)
                };
                httpRequest.Headers.Add("Token", _options.ApiToken);
                httpRequest.Headers.Add("ShopId", _options.ShopId.ToString());

                var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
                    return (true, body);

                _logger.LogWarning("GHN trả về {StatusCode}, thử lại {Attempt}/{Max}", response.StatusCode, attempt, maxAttempts);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "Lỗi kết nối GHN, thử lại {Attempt}/{Max}", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
        }

        return (false, null);
    }
}
