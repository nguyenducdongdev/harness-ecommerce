using Harness.Modules.Payment.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Payment;

/// <summary>Điểm đăng ký dịch vụ của module Payment — gọi từ Program.cs.</summary>
public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services, IConfiguration configuration)
    {
        // VNPay sandbox: dựng URL + xác thực chữ ký
        services.Configure<VnPayOptions>(configuration.GetSection(VnPayOptions.SectionName));
        services.AddSingleton<VnPayService>();
        return services;
    }
}
