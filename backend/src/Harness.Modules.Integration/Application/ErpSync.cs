using System.Text.Json;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Integration.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Harness.Modules.Integration.Application;

/// <summary>
/// Handler đồng bộ một loại integration event sang ERP. Mỗi handler nhận payload JSON
/// (đã publish từ outbox qua RabbitMQ) và sinh tác dụng phụ lên bảng ERP tương ứng.
/// </summary>
public interface IErpSyncHandler
{
    string EventType { get; }
    Task HandleAsync(Guid eventId, string payload, IHarnessDbContext db, CancellationToken cancellationToken);
}

internal static class ErpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

// ===== Payload deserialize (khớp shape các *IntegrationEvent đã publish) =====
internal record OrderCreatedPayload(Guid OrderId, string OrderNumber, decimal TotalAmount, string CustomerPhone, string DeliveryMethod, string PaymentMethod);
internal record OrderStatusChangedPayload(Guid OrderId, string OrderNumber, string NewStatus);
internal record PaymentSucceededPayload(Guid OrderId, string Provider, decimal Amount);

public class ErpOrderSyncHandler : IErpSyncHandler
{
    public string EventType => "order.created";

    public async Task HandleAsync(Guid eventId, string payload, IHarnessDbContext db, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<OrderCreatedPayload>(payload, ErpJson.Options)
            ?? throw new InvalidOperationException("Payload order.created không hợp lệ.");

        var exists = await db.Set<ErpSalesOrder>().AnyAsync(o => o.OrderId == evt.OrderId, cancellationToken);
        if (exists) return;

        db.Set<ErpSalesOrder>().Add(new ErpSalesOrder
        {
            OrderId = evt.OrderId,
            ErpOrderNo = $"ERP-{evt.OrderNumber}",
            OrderNumber = evt.OrderNumber,
            CustomerPhone = evt.CustomerPhone,
            TotalAmount = evt.TotalAmount,
            PaymentMethod = evt.PaymentMethod,
            DeliveryMethod = evt.DeliveryMethod,
            Status = "Created",
            SyncedAt = DateTimeOffset.UtcNow
        });
    }
}

public class ErpOrderStatusSyncHandler : IErpSyncHandler
{
    public string EventType => "order.status-changed";

    public async Task HandleAsync(Guid eventId, string payload, IHarnessDbContext db, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<OrderStatusChangedPayload>(payload, ErpJson.Options)
            ?? throw new InvalidOperationException("Payload order.status-changed không hợp lệ.");

        var order = await db.Set<ErpSalesOrder>().FirstOrDefaultAsync(o => o.OrderId == evt.OrderId, cancellationToken);
        if (order is null) return;
        order.Status = evt.NewStatus;
    }
}

public class ErpPaymentSyncHandler : IErpSyncHandler
{
    public string EventType => "payment.succeeded";

    public async Task HandleAsync(Guid eventId, string payload, IHarnessDbContext db, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<PaymentSucceededPayload>(payload, ErpJson.Options)
            ?? throw new InvalidOperationException("Payload payment.succeeded không hợp lệ.");

        var order = await db.Set<ErpSalesOrder>().FirstOrDefaultAsync(o => o.OrderId == evt.OrderId, cancellationToken);
        if (order is null) return;
        order.Status = "Paid";
    }
}

/// <summary>
/// Điều phối consume: nhận event từ RabbitMQ (hoặc retry từ DB), chạy handler phù hợp và
/// ghi/đánh dấu ErpSyncRecord (Synced/Failed). Idempotent theo EventId.
/// </summary>
public class ErpSyncProcessor
{
    private readonly IHarnessDbContext _db;
    private readonly IReadOnlyCollection<IErpSyncHandler> _handlers;
    private readonly ILogger<ErpSyncProcessor> _logger;

    public ErpSyncProcessor(IHarnessDbContext db, IEnumerable<IErpSyncHandler> handlers, ILogger<ErpSyncProcessor> logger)
    {
        _db = db;
        _handlers = handlers.ToList();
        _logger = logger;
    }

    public async Task<ErpSyncStatus> ProcessAsync(string eventType, Guid eventId, string payload, CancellationToken cancellationToken = default)
    {
        var record = await _db.Set<ErpSyncRecord>().FirstOrDefaultAsync(r => r.EventId == eventId, cancellationToken);
        if (record is not null && record.Status == ErpSyncStatus.Synced)
            return ErpSyncStatus.Synced;

        record ??= new ErpSyncRecord { EventId = eventId, EventType = eventType, Payload = payload };
        if (record.Id == Guid.Empty || !_db.Set<ErpSyncRecord>().Local.Contains(record))
            _db.Set<ErpSyncRecord>().Add(record);

        try
        {
            var handler = _handlers.FirstOrDefault(h => h.EventType == eventType);
            if (handler is not null)
                await handler.HandleAsync(eventId, payload, _db, cancellationToken);

            record.Status = ErpSyncStatus.Synced;
            record.Error = null;
            record.ProcessedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            record.RetryCount++;
            record.Status = ErpSyncStatus.Failed;
            record.Error = ex.Message[..Math.Min(ex.Message.Length, 500)];
            _logger.LogError(ex, "ERP sync thất bại cho {EventType} (event {EventId})", eventType, eventId);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return record.Status;
    }
}

// ===== Use cases giám sát + retry ERP =====

public record ErpOrderDto(Guid Id, string ErpOrderNo, string OrderNumber, string CustomerPhone,
    decimal TotalAmount, string PaymentMethod, string DeliveryMethod, string Status, DateTimeOffset? SyncedAt);

public record GetErpOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<Harness.BuildingBlocks.Application.Common.PagedResult<ErpOrderDto>>;

public class GetErpOrdersQueryHandler : IRequestHandler<GetErpOrdersQuery, Harness.BuildingBlocks.Application.Common.PagedResult<ErpOrderDto>>
{
    private readonly IHarnessDbContext _db;
    public GetErpOrdersQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<Harness.BuildingBlocks.Application.Common.PagedResult<ErpOrderDto>> Handle(GetErpOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _db.Set<ErpSalesOrder>().AsNoTracking();

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return Harness.BuildingBlocks.Application.Common.PagedResult<ErpOrderDto>.Create(
            items.Select(o => new ErpOrderDto(o.Id, o.ErpOrderNo, o.OrderNumber, o.CustomerPhone,
                o.TotalAmount, o.PaymentMethod, o.DeliveryMethod, o.Status, o.SyncedAt)).ToList(),
            total, page, pageSize);
    }
}

public record ErpSummaryDto(long TotalOrders, long SyncedEvents, long FailedEvents, long PendingEvents);

public record GetErpSummaryQuery : IRequest<ErpSummaryDto>;

public class GetErpSummaryQueryHandler : IRequestHandler<GetErpSummaryQuery, ErpSummaryDto>
{
    private readonly IHarnessDbContext _db;
    public GetErpSummaryQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<ErpSummaryDto> Handle(GetErpSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await _db.Set<ErpSalesOrder>().LongCountAsync(cancellationToken);
        var records = _db.Set<ErpSyncRecord>();
        var synced = await records.LongCountAsync(r => r.Status == ErpSyncStatus.Synced, cancellationToken);
        var failed = await records.LongCountAsync(r => r.Status == ErpSyncStatus.Failed, cancellationToken);
        var pending = await records.LongCountAsync(r => r.Status == ErpSyncStatus.Pending, cancellationToken);
        return new ErpSummaryDto(orders, synced, failed, pending);
    }
}

/// <summary>Retry các bản ghi ERP failed (chạy lại handler từ payload đã lưu), trả về số đã thử lại.</summary>
public record RetryErpCommand : IRequest<int>;

public class RetryErpCommandHandler : IRequestHandler<RetryErpCommand, int>
{
    private readonly IHarnessDbContext _db;
    private readonly ErpSyncProcessor _processor;

    public RetryErpCommandHandler(IHarnessDbContext db, ErpSyncProcessor processor)
    {
        _db = db;
        _processor = processor;
    }

    public async Task<int> Handle(RetryErpCommand request, CancellationToken cancellationToken)
    {
        var failed = await _db.Set<ErpSyncRecord>()
            .Where(r => r.Status == ErpSyncStatus.Failed && r.RetryCount < 3)
            .OrderBy(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var record in failed)
        {
            record.Status = ErpSyncStatus.Pending;
            await _processor.ProcessAsync(record.EventType, record.EventId, record.Payload, cancellationToken);
        }

        return failed.Count;
    }
}