using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Integration.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Integration.Application;

/// <summary>Trạng thái tổng hợp của event outbox (giám sát vận hành).</summary>
public record OutboxStatusDto(long Total, long Pending, long Processed, long Failed);

public record GetOutboxStatusQuery : IRequest<OutboxStatusDto>;

public class GetOutboxStatusQueryHandler : IRequestHandler<GetOutboxStatusQuery, OutboxStatusDto>
{
    private readonly IHarnessDbContext _db;

    public GetOutboxStatusQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<OutboxStatusDto> Handle(GetOutboxStatusQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<OutboxMessage>();
        var total = await query.LongCountAsync(cancellationToken);
        var processed = await query.LongCountAsync(m => m.ProcessedAt != null, cancellationToken);
        var failed = await query.LongCountAsync(m => m.ProcessedAt == null && m.RetryCount >= 5, cancellationToken);
        return new OutboxStatusDto(total, total - processed - failed, processed, failed);
    }
}

public record OutboxMessageDto(Guid Id, string EventType, DateTimeOffset OccurredAt,
    DateTimeOffset? ProcessedAt, string? Error, int RetryCount, string Status);

public record GetOutboxMessagesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<OutboxMessageDto>>;

public class GetOutboxMessagesQueryHandler : IRequestHandler<GetOutboxMessagesQuery, PagedResult<OutboxMessageDto>>
{
    private readonly IHarnessDbContext _db;

    public GetOutboxMessagesQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PagedResult<OutboxMessageDto>> Handle(GetOutboxMessagesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _db.Set<OutboxMessage>().AsNoTracking();

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderBy(m => m.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<OutboxMessageDto>.Create(
            items.Select(m => new OutboxMessageDto(
                m.Id, m.EventType, m.OccurredAt, m.ProcessedAt, m.Error, m.RetryCount,
                m.ProcessedAt != null ? "Processed"
                    : m.RetryCount >= 5 ? "Failed"
                    : "Pending")).ToList(),
            total, page, pageSize);
    }
}

/// <summary>Reset các outbox đạt max retry để publish lại (admin).</summary>
public record RetryFailedOutboxCommand : IRequest<int>;

public class RetryFailedOutboxCommandHandler : IRequestHandler<RetryFailedOutboxCommand, int>
{
    private readonly IHarnessDbContext _db;

    public RetryFailedOutboxCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<int> Handle(RetryFailedOutboxCommand request, CancellationToken cancellationToken)
    {
        var failed = await _db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount >= 5)
            .ToListAsync(cancellationToken);

        foreach (var message in failed)
        {
            message.RetryCount = 0;
            message.Error = null;
        }

        if (failed.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return failed.Count;
    }
}

public record SyncLogDto(Guid Id, string TargetSystem, string Direction, string EventType,
    bool Success, string? Error, int RetryCount, DateTimeOffset CreatedAt);

public record GetSyncLogsQuery(string? TargetSystem = null, bool? Success = null,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<SyncLogDto>>;

public class GetSyncLogsQueryHandler : IRequestHandler<GetSyncLogsQuery, PagedResult<SyncLogDto>>
{
    private readonly IHarnessDbContext _db;

    public GetSyncLogsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PagedResult<SyncLogDto>> Handle(GetSyncLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _db.Set<IntegrationSyncLog>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.TargetSystem))
            query = query.Where(x => x.TargetSystem == request.TargetSystem);
        if (request.Success.HasValue)
            query = query.Where(x => x.Success == request.Success.Value);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<SyncLogDto>.Create(
            items.Select(x => new SyncLogDto(x.Id, x.TargetSystem, x.Direction, x.EventType,
                x.Success, x.Error, x.RetryCount, x.CreatedAt)).ToList(),
            total, page, pageSize);
    }
}
