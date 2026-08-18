using Harness.BuildingBlocks.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Api.Reporting;

/// <summary>
/// Admin Dashboard / Báo cáo vận hành — query nặng chạy bằng Dapper (Phase 3).
/// Chỉ admin (Admin/SuperAdmin/Operations) được truy cập.
/// </summary>
[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = "Admin,SuperAdmin,Operations")]
public class DashboardController : ControllerBase
{
    private readonly DashboardQueries _queries;

    public DashboardController(DashboardQueries queries) => _queries = queries;

    /// <summary>Chỉ số tổng hợp: đơn, doanh thu, khách hàng, outbox, ERP.</summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis() => Ok(ApiResponse.Ok(await _queries.GetKpisAsync()));

    /// <summary>Doanh thu theo ngày (mặc định 30 ngày).</summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] int days = 30) => Ok(ApiResponse.Ok(await _queries.GetRevenueByDayAsync(days)));

    /// <summary>Top sản phẩm bán chạy theo doanh thu.</summary>
    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] int limit = 10) => Ok(ApiResponse.Ok(await _queries.GetTopProductsAsync(limit)));

    /// <summary>Phân bổ đơn hàng theo trạng thái.</summary>
    [HttpGet("order-status")]
    public async Task<IActionResult> OrderStatus() => Ok(ApiResponse.Ok(await _queries.GetOrderStatusBreakdownAsync()));

    /// <summary>SKU tồn dưới ngưỡng (cảnh báo nhập hàng).</summary>
    [HttpGet("low-stock")]
    public async Task<IActionResult> LowStock([FromQuery] int threshold = 5) => Ok(ApiResponse.Ok(await _queries.GetLowStockAsync(threshold)));
}
