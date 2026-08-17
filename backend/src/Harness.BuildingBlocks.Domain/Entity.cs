namespace Harness.BuildingBlocks.Domain;

/// <summary>Base entity cho toàn bộ hệ thống. Hỗ trợ domain events.</summary>
public abstract class Entity<TKey> where TKey : IEquatable<TKey>
{
    public TKey Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TKey> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (EqualityComparer<TKey>.Default.Equals(Id, default!) ||
            EqualityComparer<TKey>.Default.Equals(other.Id, default!)) return false;
        return EqualityComparer<TKey>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}
