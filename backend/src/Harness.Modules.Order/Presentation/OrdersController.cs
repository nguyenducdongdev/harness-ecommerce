using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Order.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Order.Presentation;

public class OrdersController : ApiController
{
    public OrdersController(ISender mediator) : base(mediator) { }

    /// <summary>Tạo đơn hàng mới (checkout).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
    {
        var order = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetByNumber), new { orderNumber = order.OrderNumber },
            ApiResponse<object>.Ok(order, $"Đặt hàng thành công. Mã đơn: {order.OrderNumber}"));
    }

    /// <summary>Tra cứu đơn theo mã.</summary>
    [HttpGet("{orderNumber}")]
    public async Task<IActionResult> GetByNumber(string orderNumber)
    {
        var order = await Mediator.Send(new GetOrderQuery(orderNumber));
        return order is null
            ? NotFound(ApiResponse.Fail($"Không tìm thấy đơn '{orderNumber}'."))
            : Ok(ApiResponse.Ok(order));
    }

    /// <summary>Lịch sử đơn theo số điện thoại (Phase 2: thay bằng token khách hàng).</summary>
    [HttpGet("by-phone")]
    public async Task<IActionResult> GetByPhone([FromQuery] string phone, [FromQuery] int page = 1)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetOrdersByPhoneQuery(phone, page))));
}
