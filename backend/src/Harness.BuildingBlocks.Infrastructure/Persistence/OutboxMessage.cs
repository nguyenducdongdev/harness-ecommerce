using Harness.BuildingBlocks.Domain;

namespace Harness.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Outbox row: ghi cùng transaction với business data để đảm bảo event không bị mất
/// (Outbox Pattern). Hangfire job OutboxPublisherJob sẽ publish lên RabbitMQ và đánh dấu CompletedAt.
/// </summary>
public class OutboxMessage : Entity<Guid>
{
    public OutboxMessage() => Id = Guid.NewGuid();

    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
