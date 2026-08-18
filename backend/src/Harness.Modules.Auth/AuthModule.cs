using Harness.Modules.Auth.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<JwtTokenService>();

        // Đăng ký handler/validator tự scan qua moduleAssemblies trong Program.cs (MediatR),
        // vì vậy module này chỉ cần đăng ký dịch vụ Infrastructure riêng.
        return services;
    }
}