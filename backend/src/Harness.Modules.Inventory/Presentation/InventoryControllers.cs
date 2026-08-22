using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Inventory.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Inventory.Presentation;

public class StocksController : ApiController
{
    public StocksController(ISender mediator) : base(mediator) { }

    /// <summary>Xem tồn kho 1 SKU ở tất cả kho/showroom.</summary>
    [HttpGet("{variantSku}")]
    public async Task<IActionResult> GetStock(string variantSku)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetStockQuery(variantSku))));

    /// <summary>Điều chỉnh tồn kho (admin/nhân viên kho).</summary>
    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,SuperAdmin,Warehouse")]
    public async Task<IActionResult> Adjust([FromBody] AdjustStockCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(ApiResponse.Ok(result, "Đã cập nhật tồn kho."));
    }

    /// <summary>Khai báo/nhập tồn kho khởi tạo cho (kho × SKU) — đưa khả dụng về đúng giá trị.</summary>
    [HttpPost("set")]
    [Authorize(Roles = "Admin,SuperAdmin,Warehouse")]
    public async Task<IActionResult> Set([FromBody] SetStockCommand command)
        => Ok(ApiResponse.Ok(await Mediator.Send(command), "Đã khai báo tồn kho."));

    /// <summary>Giữ chỗ tồn kho theo showroom khi có đơn (available → reserved).</summary>
    [HttpPost("reserve")]
    [Authorize(Roles = "Admin,SuperAdmin,Warehouse")]
    public async Task<IActionResult> Reserve([FromBody] ReserveStockCommand command)
        => Ok(ApiResponse.Ok(await Mediator.Send(command), "Đã giữ chỗ tồn kho."));

    /// <summary>Hoàn lại tồn kho giữ chỗ khi hủy đơn (reserved → available).</summary>
    [HttpPost("release")]
    [Authorize(Roles = "Admin,SuperAdmin,Warehouse")]
    public async Task<IActionResult> Release([FromBody] ReleaseStockCommand command)
        => Ok(ApiResponse.Ok(await Mediator.Send(command), "Đã hoàn lại tồn kho giữ chỗ."));

    /// <summary>Chuyển kho giữa showroom/kho (TransferOut kho nguồn + TransferIn kho đích trong 1 giao dịch).</summary>
    [HttpPost("transfer")]
    [Authorize(Roles = "Admin,SuperAdmin,Warehouse")]
    public async Task<IActionResult> Transfer([FromBody] TransferStockCommand command)
        => Ok(ApiResponse<object>.Ok(await Mediator.Send(command), "Đã chuyển kho."));
}

public class WarehousesController : ApiController
{
    public WarehousesController(ISender mediator) : base(mediator) { }

    /// <summary>Danh sách kho/showroom.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetWarehousesQuery(onlyActive))));

    /// <summary>M15 — Tìm kho/showroom gần nhất với toạ độ giao hàng (Haversine).</summary>
    [HttpGet("nearest")]
    public async Task<IActionResult> FindNearest(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] string? sku = null,
        [FromQuery] int quantity = 1)
    {
        var result = await Mediator.Send(new FindNearestWarehouseQuery(lat, lng, sku, quantity));
        return result is null
            ? NotFound(ApiResponse.Fail("Không có kho active có toạ độ để phân bổ."))
            : Ok(ApiResponse.Ok(result));
    }
}
