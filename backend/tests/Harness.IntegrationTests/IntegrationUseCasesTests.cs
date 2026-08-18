using Harness.Api.Persistence;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Integration.Application;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Harness.IntegrationTests;

/// <summary>
/// Integration test cho module Integration: outbox status/list/retry + sync logs.
/// Dùng EF InMemory như CatalogDbTests (model map từ AppDbContext, không cần PostgreSQL).
/// </summary>
public class IntegrationUseCasesTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task OutboxStatus_CountsProcessedPendingFailed()
    {
        using var db = CreateDb();
        var outbox = db.Set<OutboxMessage>();
        outbox.AddRange(
            new OutboxMessage { EventType = "OrderCreatedIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-3), ProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-2) },
            new OutboxMessage { EventType = "OrderCreatedIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-2), ProcessedAt = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new OutboxMessage { EventType = "StockChangedIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1), RetryCount = 2 },
            new OutboxMessage { EventType = "PaymentSucceededIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow, RetryCount = 5, Error = "timeout" });
        await db.SaveChangesAsync();

        var handler = new GetOutboxStatusQueryHandler(db);
        var status = await handler.Handle(new GetOutboxStatusQuery(), CancellationToken.None);

        Assert.Equal(4, status.Total);
        Assert.Equal(2, status.Processed);
        Assert.Equal(1, status.Failed);
        Assert.Equal(1, status.Pending);
    }

    [Fact]
    public async Task RetryFailed_ResetsFailedMessagesToPending()
    {
        using var db = CreateDb();
        var outbox = db.Set<OutboxMessage>();
        outbox.AddRange(
            new OutboxMessage { EventType = "StockChangedIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-5), RetryCount = 5, Error = "broker down" },
            new OutboxMessage { EventType = "StockChangedIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-4), RetryCount = 3 },
            new OutboxMessage { EventType = "OrderCreatedIntegrationEvent", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-3), ProcessedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var failedId = (await db.Set<OutboxMessage>().SingleAsync(m => m.RetryCount == 5)).Id;

        var handler = new RetryFailedOutboxCommandHandler(db);
        var count = await handler.Handle(new RetryFailedOutboxCommand(), CancellationToken.None);

        Assert.Equal(1, count);
        var reset = await db.Set<OutboxMessage>().SingleAsync(m => m.Id == failedId);
        Assert.Equal(0, reset.RetryCount);
        Assert.Null(reset.Error);
        Assert.Null(reset.ProcessedAt);
    }

    [Fact]
    public async Task GetOutboxMessages_PagesByOccurredAt()
    {
        using var db = CreateDb();
        var outbox = db.Set<OutboxMessage>();
        for (var i = 0; i < 25; i++)
            outbox.Add(new OutboxMessage { EventType = $"Evt{i}", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-i) });
        await db.SaveChangesAsync();

        var handler = new GetOutboxMessagesQueryHandler(db);
        var page = await handler.Handle(new GetOutboxMessagesQuery(Page: 1, PageSize: 10), CancellationToken.None);

        Assert.Equal(25, page.TotalCount);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
        Assert.All(page.Items, m => Assert.NotEqual("Failed", m.Status));
    }

    [Fact]
    public async Task GetSyncLogs_FiltersByTargetSystemAndSuccess()
    {
        using var db = CreateDb();
        var logs = db.Set<IntegrationSyncLog>();
        logs.AddRange(
            new IntegrationSyncLog { TargetSystem = "erp", Direction = "out", EventType = "OrderCreatedIntegrationEvent", Success = true, CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) },
            new IntegrationSyncLog { TargetSystem = "erp", Direction = "out", EventType = "StockChangedIntegrationEvent", Success = false, Error = "422", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new IntegrationSyncLog { TargetSystem = "shopee", Direction = "in", EventType = "OrderSync", Success = true, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var handler = new GetSyncLogsQueryHandler(db);

        var erp = await handler.Handle(new GetSyncLogsQuery(TargetSystem: "erp"), CancellationToken.None);
        Assert.Equal(2, erp.TotalCount);

        var erpFailed = await handler.Handle(new GetSyncLogsQuery(TargetSystem: "erp", Success: false), CancellationToken.None);
        var item = Assert.Single(erpFailed.Items);
        Assert.Equal("StockChangedIntegrationEvent", item.EventType);

        var all = await handler.Handle(new GetSyncLogsQuery(), CancellationToken.None);
        Assert.Equal(3, all.TotalCount);
    }
}
