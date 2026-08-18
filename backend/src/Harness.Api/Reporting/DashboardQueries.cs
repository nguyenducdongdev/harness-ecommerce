using Dapper;
using Npgsql;

namespace Harness.Api.Reporting;

// ===== DTO trả về cho Admin Dashboard (Báo cáo vận hành) =====

/// <summary>Chỉ số vận hành tổng hợp (đơn, doanh thu, outbox, ERP).</summary>
public record DashboardKpis(
    long TotalOrders,
    decimal TotalRevenue,
    decimal RevenueThisMonth,
    decimal AvgOrderValue,
    long TotalCustomers,
    long PaidOrders,
    long PendingOutbox,
    long FailedOutbox,
    long ErpFailed);

public record RevenueByDayItem(string Date, int OrderCount, decimal Revenue);

public record TopProductItem(int ProductId, string ProductName, string VariantSku, long TotalQty, decimal TotalRevenue);

public record OrderStatusItem(int Status, long Count);

public record LowStockItem(string VariantSku, string ProductName, string WarehouseName, int QuantityAvailable);

/// <summary>
/// Query "nặng" cho Admin Dashboard chạy bằng Dapper + NpgsqlConnection trực tiếp
/// (Phase 3 theo plan.md — EF Core vẫn dùng cho luồng nghiệp vụ, Dapper cho báo cáo
/// join nhiều bảng / multi-schema).
/// Lưu ý: migration EF tạo cột PascalCase nên mọi identifier phải quote trong SQL.
/// </summary>
public class DashboardQueries
{
    private readonly string _connectionString;

    public DashboardQueries(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:PostgreSQL trong cấu hình.");
    }

    private NpgsqlConnection Open() => new(_connectionString);

    /// <summary>Chỉ số tổng hợp: đơn hàng, doanh thu (loại trừ hủy/hoàn), khách, outbox, ERP.</summary>
    public async Task<DashboardKpis> GetKpisAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM orders.orders) AS "TotalOrders",
                (SELECT COALESCE(SUM("ItemsTotal" + "ShippingFee" - "DiscountAmount"), 0)
                   FROM orders.orders WHERE "Status" NOT IN (6, 7)) AS "TotalRevenue",
                (SELECT COALESCE(SUM("ItemsTotal" + "ShippingFee" - "DiscountAmount"), 0)
                   FROM orders.orders
                  WHERE "Status" NOT IN (6, 7) AND "CreatedAt" >= date_trunc('month', now())) AS "RevenueThisMonth",
                (SELECT COALESCE(AVG("ItemsTotal" + "ShippingFee" - "DiscountAmount"), 0)
                   FROM orders.orders WHERE "Status" NOT IN (6, 7)) AS "AvgOrderValue",
                (SELECT COUNT(*) FROM customer.customers) AS "TotalCustomers",
                (SELECT COUNT(DISTINCT "OrderId") FROM orders.payment_transactions WHERE "Status" = 2) AS "PaidOrders",
                (SELECT COUNT(*) FROM integration.event_outbox WHERE "ProcessedAt" IS NULL AND "Error" IS NULL) AS "PendingOutbox",
                (SELECT COUNT(*) FROM integration.event_outbox WHERE "Error" IS NOT NULL) AS "FailedOutbox",
                (SELECT COUNT(*) FROM integration.erp_sync_records WHERE "Status" = 2) AS "ErpFailed"
            """;

        await using var connection = Open();
        return await connection.QuerySingleAsync<DashboardKpis>(sql);
    }

    /// <summary>Doanh thu + số đơn theo ngày trong N ngày gần nhất (loại trừ hủy/hoàn tiền).</summary>
    public async Task<IReadOnlyList<RevenueByDayItem>> GetRevenueByDayAsync(int days, CancellationToken ct = default)
    {
        const string sql = """
            SELECT to_char("CreatedAt"::date, 'YYYY-MM-DD') AS "Date",
                   COUNT(*)::int AS "OrderCount",
                   COALESCE(SUM("ItemsTotal" + "ShippingFee" - "DiscountAmount"), 0) AS "Revenue"
              FROM orders.orders
             WHERE "Status" NOT IN (6, 7) AND "CreatedAt" >= now() - make_interval(days => @Days)
             GROUP BY "CreatedAt"::date
             ORDER BY "CreatedAt"::date
            """;

        await using var connection = Open();
        var rows = await connection.QueryAsync<RevenueByDayItem>(sql, new { Days = Math.Clamp(days, 1, 365) });
        return rows.AsList();
    }

    /// <summary>Top sản phẩm bán chạy theo doanh thu (loại trừ hủy/hoàn tiền).</summary>
    public async Task<IReadOnlyList<TopProductItem>> GetTopProductsAsync(int limit, CancellationToken ct = default)
    {
        const string sql = """
            SELECT i."ProductId" AS "ProductId",
                   i."ProductName" AS "ProductName",
                   i."VariantSku" AS "VariantSku",
                   SUM(i."Quantity")::bigint AS "TotalQty",
                   SUM(i."UnitPrice" * i."Quantity") AS "TotalRevenue"
              FROM orders.order_items i
              JOIN orders.orders o ON o."Id" = i."OrderId"
             WHERE o."Status" NOT IN (6, 7)
             GROUP BY i."ProductId", i."ProductName", i."VariantSku"
             ORDER BY "TotalRevenue" DESC
             LIMIT @Limit
            """;

        await using var connection = Open();
        var rows = await connection.QueryAsync<TopProductItem>(sql, new { Limit = Math.Clamp(limit, 1, 50) });
        return rows.AsList();
    }

    /// <summary>Phân bổ đơn hàng theo trạng thái (enum OrderStatus).</summary>
    public async Task<IReadOnlyList<OrderStatusItem>> GetOrderStatusBreakdownAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT "Status"::int AS "Status",
                   COUNT(*)::bigint AS "Count"
              FROM orders.orders
             GROUP BY "Status"
             ORDER BY "Status"
            """;

        await using var connection = Open();
        var rows = await connection.QueryAsync<OrderStatusItem>(sql);
        return rows.AsList();
    }

    /// <summary>SKU tồn dưới ngưỡng — cảnh báo nhập hàng.</summary>
    public async Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(int threshold, CancellationToken ct = default)
    {
        const string sql = """
            SELECT s."VariantSku" AS "VariantSku",
                   p."Name" AS "ProductName",
                   w."Name" AS "WarehouseName",
                   s."QuantityAvailable" AS "QuantityAvailable"
              FROM inventory.stock_levels s
              JOIN inventory.warehouses w ON w."Id" = s."WarehouseId"
              JOIN catalog.product_variants pv ON pv."Sku" = s."VariantSku"
              JOIN catalog.products p ON p."Id" = pv."ProductId"
             WHERE s."QuantityAvailable" <= @Threshold
             ORDER BY s."QuantityAvailable" ASC
             LIMIT 50
            """;

        await using var connection = Open();
        var rows = await connection.QueryAsync<LowStockItem>(sql, new { Threshold = Math.Max(0, threshold) });
        return rows.AsList();
    }
}
