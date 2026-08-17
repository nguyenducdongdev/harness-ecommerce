using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Promotion.Application;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Promotion.Presentation;

/// <summary>API quản lý flash sale.</summary>
public class FlashSalesController : ApiController
{
    public FlashSalesController(ISender mediator) : base(mediator) { }

    /// <summary>Tạo flash sale mới (admin).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateFlashSaleCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(new { id }, "Đã tạo flash sale."));
    }

    /// <summary>Thêm sản phẩm vào flash sale (admin).</summary>
    [HttpPost("{flashSaleId:int}/items")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem(int flashSaleId, [FromBody] AddFlashSaleItemRequest request)
    {
        var id = await Mediator.Send(new AddFlashSaleItemCommand(
            flashSaleId, request.ProductId, request.SalePrice, request.QuantityLimit));
        return Ok(ApiResponse<object>.Ok(new { id }, "Đã thêm sản phẩm vào flash sale."));
    }

    /// <summary>Lấy danh sách flash sale đang diễn ra.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FlashSaleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetActiveFlashSalesQuery())));
}

public record AddFlashSaleItemRequest(int ProductId, decimal SalePrice, int QuantityLimit);
