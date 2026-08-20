using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Organization.Application;
using Harness.Modules.Organization.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Organization.Presentation;

[ApiController]
[Route("api/v1/admin/attendance")]
public class AttendanceController : ApiController
{
    public AttendanceController(ISender mediator) : base(mediator) { }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendance(
        [FromQuery] Guid? staffId,
        [FromQuery] Guid? storeId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
    {
        var records = await Mediator.Send(new GetAttendanceQuery(staffId, storeId, fromDate, toDate));
        return Ok(ApiResponse<List<AttendanceDto>>.Ok(records));
    }

    [HttpPost("check-in")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Check-in thành công."));
    }

    [HttpPost("check-out")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        var result = await Mediator.Send(new CheckOutCommand(request.AttendanceId, request.Notes));
        return Ok(ApiResponse<bool>.Ok(result, "Check-out thành công."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveRecord([FromBody] SaveAttendanceRecordCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Lưu thông tin chấm công thành công."));
    }
}

public record CheckOutRequest(Guid AttendanceId, string? Notes);
