using Harness.BuildingBlocks.Application.Abstractions;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Harness.Modules.Integration.Infrastructure;

/// <summary>
/// Hangfire job: publish các event_outbox chưa xử lý lên RabbitMQ.
/// Đăng ký recurring trong Program.cs (mỗi 15 giây). Outbox Pattern đảm bảo event
/// không mất kể cả khi broker chết tạm thời — job sẽ retry ở chu kỳ sau.
/// </summary>
public class OutboxPublisherJob
{
    private readonly IHarnessDbContext _db;
    private readonly IEventBus _eventBus;
    private readonly ILogger<OutboxPublisherJob> _logger;

    public OutboxPublisherJob(IHarnessDbContext db, IEventBus eventBus, ILogger<OutboxPublisherJob> logger)
    {
        _db = db;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await _eventBus.PublishRawAsync(message.EventType, message.Payload, cancellationToken);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message[..Math.Min(ex.Message.Length, 500)];
                _logger.LogError(ex, "Outbox publish thất bại: {EventType} (retry {Retry})", message.EventType, message.RetryCount);
            }
        }

        if (pending.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Outbox: đã publish {Count} event", pending.Count);
        }
    }
}
