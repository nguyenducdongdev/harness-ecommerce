using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Customer.Application;
using Harness.Modules.Customer.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Http;
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

    // ===== OTP đăng nhập/đăng ký qua số điện thoại =====

    /// <summary>Gửi mã OTP về số điện thoại (sandbox: log + trả mã nếu cấu hình).</summary>
    [HttpPost("otp/request")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpCommand command)
        => Ok(ApiResponse.Ok(await Mediator.Send(command), "Đã gửi mã OTP."));

    /// <summary>Xác thực mã OTP → trả về access token phiên (dùng cho checkout / đặt lịch).</summary>
    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
    {
        var session = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(session, "Xác thực OTP thành công."));
    }

    /// <summary>Thông tin khách hàng đang đăng nhập (Authorization: Bearer &lt;token&gt;).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me([FromServices] OtpService otp, CancellationToken cancellationToken)
    {
        var token = ParseBearer(Request);
        if (string.IsNullOrEmpty(token))
            return Unauthorized(ApiResponse.Fail("Thiếu token."));

        var phone = await otp.ResolveSessionAsync(token, cancellationToken);
        if (phone is null)
            return Unauthorized(ApiResponse.Fail("Phiên không hợp lệ hoặc đã hết hạn."));

        var customer = await Mediator.Send(new GetCustomerByPhoneQuery(phone), cancellationToken);
        return customer is null
            ? NotFound(ApiResponse.Fail("Không tìm thấy khách hàng."))
            : Ok(ApiResponse.Ok(customer));
    }

    private static string? ParseBearer(HttpRequest request)
    {
        var auth = request.Headers.Authorization.ToString();
        return auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? auth["Bearer ".Length..].Trim()
            : null;
    }
}
