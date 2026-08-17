namespace Harness.BuildingBlocks.Domain;

/// <summary>Sự kiện xảy ra trong domain, xử lý đồng bộ trong cùng process.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
