using System.Net;
using System.Text.Json;
using Harness.Modules.Shipping.Application;
using Harness.Modules.Shipping.Application.Providers;
using Harness.Modules.Shipping.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Shipping.Infrastructure.Providers;

public sealed class GhtkShippingProvider : IShippingProvider
{
    private readonly HttpClient _httpClient;
    private readonly GhtkOptions _options;
    private readonly ILogger<GhtkShippingProvider> _logger;

    public Carrier Carrier => Carrier.Ghtk;

    public GhtkShippingProvider(HttpClient httpClient, IOptions<GhtkOptions> options, ILogger<GhtkShippingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ShippingFeeResult> CalculateAsync(ShippingFeeRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new ShippingFeeResult(Carrier.Ghtk, 0, "", false, "GHTK chưa được bật trong cấu hình.");

        var query = $"/services/shipment/fee?pickProvince=H%C3%A0%20N%E1%BB%99i&pickDistrict=Ho%C3%A0n%20Ki%E1%BA%BFm&province=H%C3%A0%20N%E1%BB%99i&district={request.ToDistrictId}&weight={request.WeightKg * 1000}&value={request.CodValue ?? 0}";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, query);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiToken);

                var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("fee", out var feeEl) && feeEl.TryGetProperty("fee", out var value))
                        {
                            var fee = value.GetInt32();
                            return new ShippingFeeResult(Carrier.Ghtk, fee, "2-5 ngày", true, "OK", body);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi parse phản hồi GHTK.");
                    }
                    return new ShippingFeeResult(Carrier.Ghtk, 0, "", false, "Không thể parse phản hồi GHTK.", body);
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                    return new ShippingFeeResult(Carrier.Ghtk, 0, "", false, "Yêu cầu GHTK không hợp lệ.", body);

                _logger.LogWarning("GHTK trả về {StatusCode}, thử lại {Attempt}/3", response.StatusCode, attempt);
            }
            catch (Exception ex) when (attempt < 3)
            {
                _logger.LogWarning(ex, "Lỗi kết nối GHTK, thử lại {Attempt}/3", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
        }

        return new ShippingFeeResult(Carrier.Ghtk, 0, "", false, "Không thể kết nối GHTK sau 3 lần thử.");
    }
}
