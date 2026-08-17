using Harness.Modules.Customer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Customer;

/// <summary>Điểm đăng ký dịch vụ của module Customer — gọi từ Program.cs.</summary>
public static class CustomerModule
{
    public static IServiceCollection AddCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        // OTP đăng nhập/đăng ký qua số điện thoại (cache-based, sandbox)
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.AddScoped<OtpService>();
        return services;
    }
}
