using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Shipping.Application;
using Harness.Modules.Shipping.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Shipping.Presentation;

/// <summary>API vận chuyển công khai (ước tính phí ship).</summary>
public class ShippingQuotesController : ApiController
{
    public ShippingQuotesController(ISender mediator) : base(mediator) { }

    /// <summary>Ước tính phí vận chuyển theo thể tích (volumetric weight W×D×H/6000).</summary>
    [HttpGet("quote")]
    [ProducesResponseType(typeof(ApiResponse<ShippingQuote>), StatusCodes.Status200OK)]
    public IActionResult GetQuote(
        [FromServices] ShippingCalculator calculator,
        [FromQuery] int widthCm,
        [FromQuery] int depthCm,
        [FromQuery] int heightCm,
        [FromQuery] double weightKg = 0,
        [FromQuery] string zone = "noi-thanh")
    {
        if (widthCm <= 0 || depthCm <= 0 || heightCm <= 0)
            return BadRequest(ApiResponse.Fail("Kích thước phải lớn hơn 0."));

        var validZones = new[] { "noi-thanh", "ngoai-thanh", "tinh" };
        if (!validZones.Contains(zone))
            return BadRequest(ApiResponse.Fail("Khu vực không hợp lệ (noi-thanh, ngoai-thanh, tinh)."));

        var quote = calculator.Calculate(widthCm, depthCm, heightCm, weightKg, zone);
        return Ok(ApiResponse.Ok(quote));
    }
}

/// <summary>API quản trị vận chuyển (tạo lô hàng, cập nhật trạng thái).</summary>
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
