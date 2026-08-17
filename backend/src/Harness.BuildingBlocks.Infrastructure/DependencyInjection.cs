using Harness.BuildingBlocks.Application.Abstractions;
using Harness.BuildingBlocks.Infrastructure.Caching;
using Harness.BuildingBlocks.Infrastructure.Events;
using Harness.BuildingBlocks.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Harness.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "harness:";
        });

        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        services.AddSingleton<ICacheService, RedisCacheService>();

        // File storage: local filesystem (Dev/Staging) hoặc MinIO (Production)
        // Chọn provider qua config: FileStorage:Provider = "local" | "minio"
        var fileStorageProvider = configuration.GetSection("FileStorage")["Provider"]?.Trim().ToLowerInvariant() ?? "local";
        services.Configure<LocalFileStorageOptions>(configuration.GetSection(LocalFileStorageOptions.SectionName));
        if (fileStorageProvider == "minio")
        {
            services.Configure<MinioStorageOptions>(configuration.GetSection(MinioStorageOptions.SectionName));
            services.AddScoped<IFileStorage, MinioFileStorage>();
        }
        else
        {
            services.AddScoped<IFileStorage, LocalFileStorage>();
        }

        return services;
    }
}
