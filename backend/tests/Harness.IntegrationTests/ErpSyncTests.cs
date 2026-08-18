using Harness.Api.Persistence;
using Harness.Modules.Integration.Application;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Harness.IntegrationTests;

/// <summary>
/// Integration test cho ERP sync: ErpSyncProcessor consume event → handler → bảng erp_sales_orders
/// + erp_sync_records, query giám sát + retry. Dùng EF InMemory như các test khác.
/// </summary>
public class ErpSyncTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ErpSyncProcessor CreateProcessor(AppDbContext db, params IErpSyncHandler[] handlers)
    {
        var all = handlers.Length > 0
            ? handlers
            : new IErpSyncHandler[]
            {
                new ErpOrderSyncHandler(),
                new ErpOrderStatusSyncHandler(),
                new ErpPaymentSyncHandler(),
            };
        return new ErpSyncProcessor(db, all, NullLogger<ErpSyncProcessor>.Instance);
    }

    [Fact]
    public async Task OrderCreated_MapsToErpSalesOrder()
    {
        using var db = CreateDb();
        var orderId = Guid.NewGuid();
        var payload = $@"{{ ""OrderId"": ""{orderId}"", ""OrderNumber"": ""HD0001"", ""TotalAmount"": 1250000, ""CustomerPhone"": ""0901234567"", ""DeliveryMethod"": ""noithanh"", ""PaymentMethod"": ""cod"" }}";

        var processor = CreateProcessor(db);
        var status = await processor.ProcessAsync("order.created", Guid.NewGuid(), payload);

        Assert.Equal(ErpSyncStatus.Synced, status);
        var order = await db.Set<ErpSalesOrder>().SingleAsync(o => o.OrderId == orderId);
        Assert.Equal("ERP-HD0001", order.ErpOrderNo);
        Assert.Equal(1250000m, order.TotalAmount);
        Assert.Equal("cod", order.PaymentMethod);
        Assert.Equal("Created", order.Status);
        Assert.NotNull(order.SyncedAt);
    }

    [Fact]
    public async Task OrderStatusChanged_UpdatesExistingOrder()
    {
        using var db = CreateDb();
        var orderId = Guid.NewGuid();
        db.Set<ErpSalesOrder>().Add(new ErpSalesOrder
        {
            OrderId = orderId,
            ErpOrderNo = "ERP-HD0002",
            OrderNumber = "HD0002",
            CustomerPhone = "0901234567",
            TotalAmount = 500000m,
            PaymentMethod = "cod",
            DeliveryMethod = "noithanh",
            Status = "Created",
        });
        await db.SaveChangesAsync();

        var processor = CreateProcessor(db);
        var payload = $@"{{ ""OrderId"": ""{orderId}"", ""OrderNumber"": ""HD0002"", ""NewStatus"": ""Paid"" }}";
        await processor.ProcessAsync("order.status-changed", Guid.NewGuid(), payload);

        var order = await db.Set<ErpSalesOrder>().SingleAsync(o => o.OrderId == orderId);
        Assert.Equal("Paid", order.Status);
    }

    [Fact]
    public async Task InvalidPayload_MarksSyncRecordFailed()
    {
        using var db = CreateDb();
        var processor = CreateProcessor(db);
        var status = await processor.ProcessAsync("order.created", Guid.NewGuid(), "không-phải-json");

        Assert.Equal(ErpSyncStatus.Failed, status);
        var record = await db.Set<ErpSyncRecord>().SingleAsync();
        Assert.Equal(ErpSyncStatus.Failed, record.Status);
        Assert.Equal(1, record.RetryCount);
        Assert.NotNull(record.Error);
    }

    [Fact]
    public async Task GetErpOrders_PagesResults()
    {
        using var db = CreateDb();
        for (var i = 0; i < 25; i++)
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
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
            });
        }
        await db.SaveChangesAsync();

        var handler = new GetErpOrdersQueryHandler(db);
        var page = await handler.Handle(new GetErpOrdersQuery(Page: 1, PageSize: 10), CancellationToken.None);

        Assert.Equal(25, page.TotalCount);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task GetErpSummary_CountsOrdersAndSyncRecords()
    {
        using var db = CreateDb();
        db.Set<ErpSalesOrder>().Add(new ErpSalesOrder
        {
            OrderId = Guid.NewGuid(),
            ErpOrderNo = "ERP-HD0100",
            OrderNumber = "HD0100",
            CustomerPhone = "0901234567",
            TotalAmount = 200000m,
            PaymentMethod = "vnpay",
            DeliveryMethod = "noithanh",
            Status = "Paid",
        });
        db.Set<ErpSyncRecord>().AddRange(
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "order.created", Payload = "{}", Status = ErpSyncStatus.Synced },
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "order.created", Payload = "{}", Status = ErpSyncStatus.Failed, Error = "timeout" },
            new ErpSyncRecord { EventId = Guid.NewGuid(), EventType = "order.created", Payload = "{}", Status = ErpSyncStatus.Pending });
        await db.SaveChangesAsync();

        var handler = new GetErpSummaryQueryHandler(db);
        var summary = await handler.Handle(new GetErpSummaryQuery(), CancellationToken.None);

        Assert.Equal(1, summary.TotalOrders);
        Assert.Equal(1, summary.SyncedEvents);
        Assert.Equal(1, summary.FailedEvents);
        Assert.Equal(1, summary.PendingEvents);
    }
}
