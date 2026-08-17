using System.Text;
using System.Text.Json;
using Harness.BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Harness.BuildingBlocks.Infrastructure.Events;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "harness";
    public string Password { get; set; } = "harness";
    public string ExchangeName { get; set; } = "harness.events";
}

/// <summary>
/// Publish integration events lên RabbitMQ (exchange fanout durable).
/// Lưu ý: với event quan trọng, module ghi Outbox trước (OutboxExtensions.AddToOutbox);
/// RabbitMqEventBus dùng cho publish trực tiếp không đảm bảo (best-effort).
/// </summary>
public class RabbitMqEventBus : IEventBus
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBus> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqEventBus(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default) where TEvent : notnull
    {
        var eventType = typeof(TEvent).Name;
        var payload = JsonSerializer.Serialize(integrationEvent, JsonOptions);
        return PublishRawAsync(eventType, payload, cancellationToken);
    }

    public Task PublishRawAsync(string eventType, string payload, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Fanout, durable: true);

        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Type = eventType;
        properties.MessageId = Guid.NewGuid().ToString();

        channel.BasicPublish(_options.ExchangeName, routingKey: eventType, basicProperties: properties, body: body);

        _logger.LogInformation("Published {EventType} → RabbitMQ ({Exchange})", eventType, _options.ExchangeName);
        return Task.CompletedTask;
    }
}
