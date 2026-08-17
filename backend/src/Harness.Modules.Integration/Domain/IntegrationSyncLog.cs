using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Integration.Domain;

/// <summary>Nhật ký đồng bộ với hệ thống ngoài (ERP/DMS/sàn TMĐT) — phục vụ đối soát và debug.</summary>
public class IntegrationSyncLog : Entity<Guid>
{
    public string TargetSystem { get; set; } = default!; // erp / dms / shopee / tiktok
    public string Direction { get; set; } = default!;   // out / in
    public string EventType { get; set; } = default!;
    public string? Payload { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
