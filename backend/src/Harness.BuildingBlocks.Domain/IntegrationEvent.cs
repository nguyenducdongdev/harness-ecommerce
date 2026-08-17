namespace Harness.BuildingBlocks.Domain;

/// <summary>
/// Sự kiện tích hợp: được ghi vào event_outbox (cùng transaction với business data),
/// sau đó Hangfire publish lên RabbitMQ để ERP/DMS/sản xuất hoặc module khác consume.
/// Xem Outbox Pattern trong BuildingBlocks.Infrastructure.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public abstract string EventType { get; }
}
