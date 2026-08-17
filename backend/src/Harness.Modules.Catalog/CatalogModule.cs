using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Catalog.Infrastructure.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.Modules.Catalog;

/// <summary>Điểm đăng ký dịch vụ của module Catalog — gọi từ Program.cs của Harness.Api.</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Elasticsearch: cấu hình index + dịch vụ index/tìm kiếm sản phẩm
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));

        // Đăng ký 1 instance dùng chung cho cả 2 hợp đồng (IProductIndexer + IProductSearch)
        services.AddSingleton<IProductIndexer, ProductIndexer>();
        services.AddSingleton<IProductSearch>(sp => sp.GetRequiredService<IProductIndexer>());

        // Reindex toàn bộ cho SearchController + Hangfire recurring job
        services.AddScoped<ProductReindexService>();
        services.AddTransient<ProductReindexJob>();

        return services;
    }
}

