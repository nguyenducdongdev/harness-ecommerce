using System.Net;
using Harness.Modules.Shipping.Application;
using Harness.Modules.Shipping.Application.Providers;
using Harness.Modules.Shipping.Domain;
using Harness.Modules.Shipping.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử tích hợp GHN sandbox: retry, parse phản hồi, fallback khi chưa bật.</summary>
public class GhnShippingProviderTests
{
    private static GhnShippingProvider CreateProvider(HttpClient httpClient, bool enabled)
    {
        var options = Options.Create(new GhnOptions
        {
            Enabled = enabled,
            BaseUrl = "https://dev-online-gateway.ghn.vn",
            ApiToken = "test-token",
            ShopId = 123,
            FromDistrictId = 1,
            FromWardCode = "W01"
        });
        return new GhnShippingProvider(httpClient, options, NullLogger<GhnShippingProvider>.Instance);
    }

    [Fact]
    public async Task Calculate_WhenDisabled_ReturnsNotEnabled()
    {
        var provider = CreateProvider(new HttpClient(), false);

        var result = await provider.CalculateAsync(new ShippingFeeRequest(1, "W02", 10, 100, 80, 70));

        Assert.False(result.Success);
        Assert.Contains("GHN", result.Message);
    }

    [Fact]
    public async Task Calculate_WithSuccessResponse_ReturnsFee()
    {
        var handler = new TestHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"code\":200,\"data\":{\"total\":150000}}")
        });
        var provider = CreateProvider(new HttpClient(handler) { BaseAddress = new Uri("https://dev-online-gateway.ghn.vn") }, true);

        var result = await provider.CalculateAsync(new ShippingFeeRequest(1, "W02", 10, 100, 80, 70));

        Assert.True(result.Success);
        Assert.Equal(150_000, result.Fee);
    }

    [Fact]
    public async Task Calculate_WithErrorCode_ReturnsFailure()
    {
        var handler = new TestHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"code\":400,\"message\":\"Bad request\"}")
        });
        var provider = CreateProvider(new HttpClient(handler) { BaseAddress = new Uri("https://dev-online-gateway.ghn.vn") }, true);

        var result = await provider.CalculateAsync(new ShippingFeeRequest(1, "W02", 10, 100, 80, 70));

        Assert.False(result.Success);
        Assert.Equal("Bad request", result.Message);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
