using Harness.Modules.Shipping.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Shipping;

/// <summary>Đăng ký dịch vụ Shipping module — gọi từ Program.cs.</summary>
public static class ShippingModule
{
    public static IServiceCollection AddShippingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShippingOptions>(configuration.GetSection(ShippingOptions.SectionName));
        services.AddSingleton<ShippingCalculator>();
        return services;
    }
}
