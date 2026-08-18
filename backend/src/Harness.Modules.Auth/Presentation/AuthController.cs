using System.Security.Claims;
using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Auth.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Auth.Presentation;

/// <summary>Đăng nhập & phiên admin (JWT + RBAC).</summary>
public class AuthController : ApiController
{
    public AuthController(ISender mediator) : base(mediator) { }

    /// <summary>Đăng nhập tài khoản quản trị → trả JWT (roles trong claims).</summary>
    [HttpPost("admin/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AdminLoginResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] AdminLoginCommand command)
    {
        var session = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(session, "Đăng nhập thành công."));
    }

    /// <summary>Thông tin admin đang đăng nhập (Authorization: Bearer &lt;token&gt;).</summary>
    [HttpGet("admin/me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var adminId = CurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse.Fail("Token không hợp lệ."));

        return Ok(ApiResponse.Ok(await Mediator.Send(new GetCurrentAdminQuery(adminId.Value), cancellationToken)));
    }

    /// <summary>Đổi mật khẩu admin.</summary>
    [HttpPost("admin/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordBody body, CancellationToken cancellationToken)
    {
        var adminId = CurrentAdminId();
        if (adminId is null)
            return Unauthorized(ApiResponse.Fail("Token không hợp lệ."));

        var ok = await Mediator.Send(new ChangePasswordCommand(adminId.Value, body.OldPassword, body.NewPassword), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { changed = ok }, "Đã đổi mật khẩu."));
    }

    private Guid? CurrentAdminId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public record ChangePasswordBody(string OldPassword, string NewPassword);