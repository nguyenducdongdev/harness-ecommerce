using Harness.Modules.Shipping.Application;
using Harness.Modules.Shipping.Application.Providers;
using Harness.Modules.Shipping.Infrastructure.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Shipping;

/// <summary>Đăng ký dịch vụ Shipping module — gọi từ Program.cs.</summary>
public static class ShippingModule
{
    public static IServiceCollection AddShippingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ShippingOptions>(configuration.GetSection(ShippingOptions.SectionName));
        services.Configure<GhnOptions>(configuration.GetSection(GhnOptions.SectionName));
        services.Configure<GhtkOptions>(configuration.GetSection(GhtkOptions.SectionName));

        services.AddSingleton<ShippingCalculator>();

        // HTTP clients with automatic retry for GHN/GHTK sandbox APIs
        services.AddHttpClient<GhnShippingProvider>(client =>
        {
            var options = configuration.GetSection(GhnOptions.SectionName).Get<GhnOptions>();
            if (!string.IsNullOrWhiteSpace(options?.BaseUrl))
                client.BaseAddress = new Uri(options.BaseUrl);
        });
        services.AddHttpClient<GhtkShippingProvider>(client =>
        {
            var options = configuration.GetSection(GhtkOptions.SectionName).Get<GhtkOptions>();
            if (!string.IsNullOrWhiteSpace(options?.BaseUrl))
                client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddScoped<GhnShippingProvider>();
        services.AddScoped<GhtkShippingProvider>();
        services.AddScoped<IShippingProvider>(sp => sp.GetRequiredService<GhnShippingProvider>());
        services.AddScoped<IShippingProvider>(sp => sp.GetRequiredService<GhtkShippingProvider>());

        return services;
    }
}
