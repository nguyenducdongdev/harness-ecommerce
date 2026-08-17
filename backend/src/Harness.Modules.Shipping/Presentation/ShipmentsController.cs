using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Shipping.Application;
using Harness.Modules.Shipping.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Shipping.Presentation;

public class ShipmentsController : ApiController
{
    public ShipmentsController(ISender mediator) : base(mediator) { }

    /// <summary>Tạo lô hàng cho đơn (gọi hãng vận chuyển — Phase 3 tích hợp API GHN/GHTK thật).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShipmentCommand command)
    {
        var shipment = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(shipment, "Đã tạo lô hàng."));
    }

    /// <summary>Cập nhật trạng thái vận chuyển (webhook từ GHN/GHTK).</summary>
    [HttpPut("{shipmentId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid shipmentId, [FromBody] UpdateShipmentStatusRequest request)
    {
        var shipment = await Mediator.Send(new UpdateShipmentStatusCommand(shipmentId, request.Status));
        return Ok(ApiResponse.Ok(shipment));
    }

    /// <summary>Tra cứu lô hàng theo đơn.</summary>
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var shipment = await Mediator.Send(new GetShipmentByOrderQuery(orderId));
        return shipment is null
            ? NotFound(ApiResponse.Fail("Đơn chưa có lô hàng."))
            : Ok(ApiResponse.Ok(shipment));
    }
}

public record UpdateShipmentStatusRequest(ShipmentStatus Status);
