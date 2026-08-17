namespace Harness.BuildingBlocks.Domain;

/// <summary>Entity có audit fields: ai tạo/sửa, khi nào.</summary>
public abstract class AuditableEntity<TKey> : Entity<TKey> where TKey : IEquatable<TKey>
{
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
