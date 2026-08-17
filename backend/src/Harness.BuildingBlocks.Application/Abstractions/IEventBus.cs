namespace Harness.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstraction cho việc publish integration events qua message broker.
/// Implementation: RabbitMQEventBus trong BuildingBlocks.Infrastructure.
/// Module chỉ phụ thuộc interface này, không phụ thuộc RabbitMQ.
/// </summary>
public interface IEventBus
{
    /// <summary>Publish trực tiếp lên broker (best-effort, dùng cho event không quan trọng).</summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default) where TEvent : notnull;

    /// <summary>Publish payload thô từ event_outbox (dùng bởi OutboxPublisherJob).</summary>
    Task PublishRawAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}
