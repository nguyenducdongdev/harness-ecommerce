using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Integration.Domain;

public enum ErpSyncStatus { Pending = 0, Synced = 1, Failed = 2, Ignored = 3 }

/// <summary>Phiếu bán hàng đã đồng bộ sang ERP (mô phỏng hệ thống ngoài).</summary>
public class ErpSalesOrder : Entity<Guid>
{
    public ErpSalesOrder() => Id = Guid.NewGuid();

    public Guid OrderId { get; set; }
    public string ErpOrderNo { get; set; } = default!;   // mã nội bộ ERP (VD: ERP-HD260818-0001)
    public string OrderNumber { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public string DeliveryMethod { get; set; } = default!;
    /// <summary>Created / Paid / + trạng thái đơn đồng bộ từ order.status-changed.</summary>
    public string Status { get; set; } = "Created";
    public DateTimeOffset? SyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Bản ghi đối soát: mỗi integration event consume từ RabbitMQ được ghi nhận ở đây.</summary>
public class ErpSyncRecord : Entity<Guid>
{
    public ErpSyncRecord() => Id = Guid.NewGuid();

    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public string TargetSystem { get; set; } = "erp";
    public ErpSyncStatus Status { get; set; } = ErpSyncStatus.Pending;
    public string Payload { get; set; } = default!;
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
}