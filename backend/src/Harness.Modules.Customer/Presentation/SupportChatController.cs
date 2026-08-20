using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Customer.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Customer.Presentation;

[ApiController]
[Route("api/v1/support/chat")]
public class SupportChatController : ApiController
{
    public SupportChatController(ISender mediator) : base(mediator)
    {
    }

    /// <summary>Bắt đầu phiên tư vấn chat trực tuyến</summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession([FromBody] StartChatSessionCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(result, "Tạo phiên chat tư vấn thành công"));
    }

    /// <summary>Lấy danh sách tin nhắn của 1 phiên chat</summary>
    [HttpGet("sessions/{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetChatMessagesQuery(id), ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>Gửi tin nhắn vào phiên chat (fallback REST)</summary>
    [HttpPost("sessions/{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendChatMessageRequest req, CancellationToken ct)
    {
        var command = new SendChatMessageCommand(id, req.SenderType, req.SenderName, req.MessageText, req.SenderId);
        var result = await Mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(result, "Gửi tin nhắn thành công"));
    }

    /// <summary>[Admin] Lấy danh sách các phiên chat tư vấn</summary>
    [HttpGet("sessions")]
    [Authorize(Roles = "Admin,SuperAdmin,CustomerService,Operations")]
    public async Task<IActionResult> GetSessions([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetChatSessionsQuery(status, page, pageSize), ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>[Admin] Tiếp nhận phiên tư vấn</summary>
    [HttpPost("sessions/{id:guid}/assign")]
    [Authorize(Roles = "Admin,SuperAdmin,CustomerService,Operations")]
    public async Task<IActionResult> AssignSession(Guid id, [FromBody] AssignSessionRequest req, CancellationToken ct)
    {
        var result = await Mediator.Send(new AssignChatSessionCommand(id, req.AgentId, req.AgentName), ct);
        return Ok(ApiResponse<object>.Ok(result, $"Tư vấn viên {req.AgentName} đã nhận phiên chat"));
    }

    /// <summary>[Admin/Customer] Kết thúc phiên tư vấn</summary>
    [HttpPost("sessions/{id:guid}/close")]
    public async Task<IActionResult> CloseSession(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new CloseChatSessionCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(result, "Đã kết thúc phiên chat"));
    }
}

public record SendChatMessageRequest(string SenderType, string SenderName, string MessageText, string? SenderId = null);
public record AssignSessionRequest(Guid AgentId, string AgentName);

