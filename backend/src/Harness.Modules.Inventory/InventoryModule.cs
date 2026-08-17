using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Inventory;

/// <summary>Điểm đăng ký dịch vụ của module Inventory — gọi từ Program.cs của Harness.Api.</summary>
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Các handler (MediatR) dùng IHarnessDbContext đã được Harness.Api đăng ký.
        // Tại đây có thể bổ sung dịch vụ đặc thù của Inventory sau này (sync DMS/ERP, reservation policies...).
        return services;
    }
}
