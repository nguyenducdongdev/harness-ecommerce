using System.Globalization;
using Harness.Api.Observability;
using Harness.Api.Persistence;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prometheus;
using Xunit;

namespace Harness.IntegrationTests;

/// <summary>
/// Integration test cho Observability (M11): MetricsReporter đọc dữ liệu từ InMemory DB
/// và Elasticsearch stub rồi cập nhật đúng gauge trên Prometheus registry riêng.
/// </summary>
public class MetricsReporterTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private sealed class StubProductSearch : IProductSearch
    {
        public long Count { get; init; }

        public Task<IReadOnlyList<ProductSearchDocument>> SearchProductsAsync(
            string term, int from = 0, int size = 20, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProductSearchDocument>>(Array.Empty<ProductSearchDocument>());

        public Task<long> CountProductsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Count);
    }

    [Fact]
    public async Task ReportAsync_SetsGaugesFromDbAndSearch()
    {
        using var db = CreateDb();
        db.Set<OutboxMessage>().AddRange(
            new OutboxMessage { EventType = "EvtA", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-2) },
            new OutboxMessage { EventType = "EvtB", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1) },
            new OutboxMessage { EventType = "EvtC", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-1), Error = "broker down" });

        for (var i = 0; i < 3; i++)
        {
            db.Set<ErpSalesOrder>().Add(new ErpSalesOrder
            {
                OrderId = Guid.NewGuid(),
                ErpOrderNo = $"ERP-{i:0000}",
                OrderNumber = $"HD{i:0000}",
                CustomerPhone = "0901234567",
                TotalAmount = 100000m * i,
                PaymentMethod = "cod",
                DeliveryMethod = "noithanh",
                Status = "Created",
            });
        }
        db.Set<ErpSyncRecord>().AddRange(
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "order.created", Payload = "{}", Status = ErpSyncStatus.Synced },
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "order.created", Payload = "{}", Status = ErpSyncStatus.Synced },
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "order.created", Payload = "{}", Status = ErpSyncStatus.Failed, Error = "422" },
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "payment.succeeded", Payload = "{}", Status = ErpSyncStatus.Pending });
        await db.SaveChangesAsync();

        var registry = new CollectorRegistry();
        var metrics = new HarnessMetrics(registry);
        var reporter = new MetricsReporter(db, new StubProductSearch { Count = 8 }, metrics, NullLogger<MetricsReporter>.Instance);

        await reporter.ReportAsync();

        var samples = await CollectValuesAsync(registry);
        Assert.Equal(2, samples["harness_outbox_pending"]);
        Assert.Equal(1, samples["harness_outbox_failed"]);
        Assert.Equal(3, samples["harness_erp_orders_total"]);
        Assert.Equal(2, samples["harness_erp_sync_records_total{status=\"Synced\"}"]);
        Assert.Equal(1, samples["harness_erp_sync_records_total{status=\"Failed\"}"]);
        Assert.Equal(1, samples["harness_erp_sync_records_total{status=\"Pending\"}"]);
        Assert.Equal(8, samples["harness_products_indexed"]);
    }

    [Fact]
    public async Task ReportAsync_OverwritesPreviousValues()
    {
        using var db = CreateDb();
        db.Set<OutboxMessage>().Add(new OutboxMessage { EventType = "EvtA", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var registry = new CollectorRegistry();
        var metrics = new HarnessMetrics(registry);
        var reporter = new MetricsReporter(db, new StubProductSearch { Count = 0 }, metrics, NullLogger<MetricsReporter>.Instance);

        await reporter.ReportAsync();
        Assert.Equal(1, (await CollectValuesAsync(registry))["harness_outbox_pending"]);

        db.Set<OutboxMessage>().Add(new OutboxMessage { EventType = "EvtB", Payload = "{}", OccurredAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        await reporter.ReportAsync();
        Assert.Equal(2, (await CollectValuesAsync(registry))["harness_outbox_pending"]);
    }

    /// <summary>Export registry sang Prometheus text format (đúng định dạng /metrics) rồi parse thành map.</summary>
    private static async Task<Dictionary<string, double>> CollectValuesAsync(CollectorRegistry registry)
    {
        using var stream = new MemoryStream();
        await registry.CollectAndExportAsTextAsync(stream);
        stream.Position = 0;

        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        var result = new Dictionary<string, double>();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            var parts = trimmed.Split(' ');
            if (parts.Length != 2) continue;
            result[parts[0]] = double.Parse(parts[1], CultureInfo.InvariantCulture);
        }

        return result;
    }
}
