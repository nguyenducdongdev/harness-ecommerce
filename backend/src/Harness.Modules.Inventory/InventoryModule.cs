using Harness.BuildingBlocks.Application.Abstractions;
using Harness.Modules.Inventory.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Inventory;

/// <summary>Điểm đăng ký dịch vụ của module Inventory — gọi từ Program.cs của Harness.Api.</summary>
public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        // M15: Warehouse auto-allocation (tìm kho gần nhất có đủ tồn) — dùng cho Order create.
        services.AddScoped<IWarehouseAllocator, NearestWarehouseAllocator>();
        return services;
    }
}
