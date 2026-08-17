using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Customer.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Customer.Presentation;

public class CustomersController : ApiController
{
    public CustomersController(ISender mediator) : base(mediator) { }

    /// <summary>Đăng ký khách hàng mới.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerCommand command)
    {
        var customer = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(customer, "Đăng ký thành công."));
    }

    /// <summary>Tra cứu khách theo SĐT (Phase 2: yêu cầu token).</summary>
    [HttpGet("by-phone")]
    public async Task<IActionResult> GetByPhone([FromQuery] string phone)
    {
        var customer = await Mediator.Send(new GetCustomerByPhoneQuery(phone));
        return customer is null
            ? NotFound(ApiResponse.Fail("Không tìm thấy khách hàng."))
            : Ok(ApiResponse.Ok(customer));
    }
}
