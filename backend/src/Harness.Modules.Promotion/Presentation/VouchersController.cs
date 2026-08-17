using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Promotion.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Promotion.Presentation;

public class VouchersController : ApiController
{
    public VouchersController(ISender mediator) : base(mediator) { }

    /// <summary>Kiểm tra voucher cho đơn hàng (gọi khi checkout).</summary>
    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string code, [FromQuery] decimal orderAmount)
        => Ok(ApiResponse.Ok(await Mediator.Send(new ValidateVoucherQuery(code, orderAmount))));

    /// <summary>Tạo voucher mới (admin).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVoucherCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(new { id }, "Đã tạo voucher."));
    }
}
