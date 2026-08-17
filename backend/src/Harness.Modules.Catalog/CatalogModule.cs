using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Catalog;

/// <summary>Điểm đăng ký dịch vụ của module Catalog — gọi từ Program.cs của Harness.Api.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        // Hiện tại handler được MediatR scan tự động. Đ-place đăng ký dịch vụ riêng của module ở đây
        // (ví dụ: Elasticsearch indexer, MinIO image service ở giai đoạn tiếp theo).
        return services;
    }
}
