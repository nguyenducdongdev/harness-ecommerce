using Prometheus;

namespace Harness.Api.Observability;

/// <summary>
/// Bộ metric nghiệp vụ của Harness, expose qua /metrics để Prometheus scrape.
/// Dùng CollectorRegistry riêng (test truyền registry mới) — production dùng Metrics.DefaultRegistry
/// chung với HTTP metrics của prometheus-net.AspNetCore.
/// </summary>
public sealed class HarnessMetrics
{
    public HarnessMetrics(CollectorRegistry? registry = null)
    {
        var factory = Metrics.WithCustomRegistry(registry ?? Metrics.DefaultRegistry);

        OutboxPending = factory.CreateGauge(
            "harness_outbox_pending",
            "Số integration event đang chờ publish từ outbox (ProcessedAt null, chưa lỗi).");
        OutboxFailed = factory.CreateGauge(
            "harness_outbox_failed",
            "Số integration event outbox publish/processing thất bại (có Error).");
        ErpOrders = factory.CreateGauge(
            "harness_erp_orders_total",
            "Tổng phiếu bán đã đồng bộ sang ERP (erp_sales_orders).");
        ErpSyncRecords = factory.CreateGauge(
            "harness_erp_sync_records_total",
            "Bản ghi đối soát đồng bộ ERP theo trạng thái (erp_sync_records).",
            "status");
        ProductsIndexed = factory.CreateGauge(
            "harness_products_indexed",
            "Số sản phẩm đang có trong index Elasticsearch (best-effort, 0 nếu ES không sẵn sàng).");
    }

    public Gauge OutboxPending { get; }
    public Gauge OutboxFailed { get; }
    public Gauge ErpOrders { get; }
    public Gauge ErpSyncRecords { get; }
    public Gauge ProductsIndexed { get; }
}
