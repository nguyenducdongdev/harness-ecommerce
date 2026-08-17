using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Inventory.Application;
using MediatR;
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
    public async Task<IActionResult> Adjust([FromBody] AdjustStockCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(ApiResponse.Ok(result, "Đã cập nhật tồn kho."));
    }
}

public class WarehousesController : ApiController
{
    public WarehousesController(ISender mediator) : base(mediator) { }

    /// <summary>Danh sách kho/showroom.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetWarehousesQuery(onlyActive))));
}
