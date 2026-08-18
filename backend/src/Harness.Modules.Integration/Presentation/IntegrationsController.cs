using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Integration.Application;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Integration.Presentation;

/// <summary>Giám sát vận hành Integration: event outbox + nhật ký đồng bộ hệ thống ngoài (admin/ops).</summary>
[Authorize(Roles = "Admin,SuperAdmin,Operations")]
public class IntegrationsController : ApiController
{
    public IntegrationsController(ISender mediator) : base(mediator) { }

    /// <summary>Trạng thái tổng hợp event outbox (total / pending / processed / failed).</summary>
    [HttpGet("outbox/status")]
    [ProducesResponseType(typeof(ApiResponse<OutboxStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxStatus()
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetOutboxStatusQuery())));

    /// <summary>Danh sách event outbox (admin).</summary>
    [HttpGet("outbox")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OutboxMessageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxMessages([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetOutboxMessagesQuery(page, pageSize))));

    /// <summary>Reset các outbox failed (đạt max retry) về hàng chờ để publish lại (admin).</summary>
    [HttpPost("outbox/retry")]
    public async Task<IActionResult> RetryFailed()
    {
        var count = await Mediator.Send(new RetryFailedOutboxCommand());
        return Ok(ApiResponse<object>.Ok(new { count }, $"Đã đưa {count} event về hàng chờ publish lại."));
    }

    /// <summary>Nhật ký đồng bộ với hệ thống ngoài (ERP / DMS / sàn TMĐT).</summary>
    [HttpGet("sync-logs")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SyncLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncLogs(
        [FromQuery] string? targetSystem = null,
        [FromQuery] bool? success = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetSyncLogsQuery(targetSystem, success, page, pageSize))));
}
