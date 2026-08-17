using Harness.BuildingBlocks.Application.Abstractions;
using Harness.BuildingBlocks.Infrastructure.Caching;
using Harness.BuildingBlocks.Infrastructure.Events;
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

        return services;
    }
}
