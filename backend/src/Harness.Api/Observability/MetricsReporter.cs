using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Harness.Api.Observability;

/// <summary>
/// Đọc dữ liệu vận hành từ DB + Elasticsearch rồi cập nhật gauge trên <see cref="HarnessMetrics"/>.
/// Mỗi nguồn có try/catch riêng: một nguồn lỗi không chặn các nguồn khác (best-effort).
/// Lớp thuần — gọi trực tiếp được trong test với InMemory DB.
/// </summary>
public class MetricsReporter
{
    private readonly IHarnessDbContext _db;
    private readonly IProductSearch _productSearch;
    private readonly HarnessMetrics _metrics;
    private readonly ILogger<MetricsReporter> _logger;

    public MetricsReporter(
        IHarnessDbContext db,
        IProductSearch productSearch,
        HarnessMetrics metrics,
        ILogger<MetricsReporter> logger)
    {
        _db = db;
        _productSearch = productSearch;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task ReportAsync(CancellationToken cancellationToken = default)
    {
        await ReportOutboxAsync(cancellationToken);
        await ReportErpAsync(cancellationToken);
        await ReportProductsAsync(cancellationToken);
    }

    private async Task ReportOutboxAsync(CancellationToken ct)
    {
        try
        {
            var messages = _db.Set<OutboxMessage>();
            _metrics.OutboxPending.Set(await messages
                .LongCountAsync(m => m.ProcessedAt == null && m.Error == null, ct));
            _metrics.OutboxFailed.Set(await messages
                .LongCountAsync(m => m.Error != null, ct));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Metrics: không đọc được outbox — {Error}", ex.Message);
        }
    }

    private async Task ReportErpAsync(CancellationToken ct)
    {
        try
        {
            _metrics.ErpOrders.Set(await _db.Set<ErpSalesOrder>().LongCountAsync(ct));

            var records = _db.Set<ErpSyncRecord>();
            _metrics.ErpSyncRecords.WithLabels("Synced").Set(await records
                .LongCountAsync(r => r.Status == ErpSyncStatus.Synced, ct));
            _metrics.ErpSyncRecords.WithLabels("Failed").Set(await records
                .LongCountAsync(r => r.Status == ErpSyncStatus.Failed, ct));
            _metrics.ErpSyncRecords.WithLabels("Pending").Set(await records
                .LongCountAsync(r => r.Status == ErpSyncStatus.Pending, ct));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Metrics: không đọc được ERP sync — {Error}", ex.Message);
        }
    }

    private async Task ReportProductsAsync(CancellationToken ct)
    {
        try
        {
            _metrics.ProductsIndexed.Set(await _productSearch.CountProductsAsync(ct));
        }
        catch (Exception ex)
        {
            // ES không sẵn sàng → gauge giữ giá trị cũ (không set 0 để tránh báo động giả khi ES down)
            _logger.LogWarning("Metrics: không đọc được Elasticsearch count — {Error}", ex.Message);
        }
    }
}
