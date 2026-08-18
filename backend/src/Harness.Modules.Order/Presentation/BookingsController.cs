using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Order.Application;
using Harness.Modules.Order.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Order.Presentation;

/// <summary>API đặt lịch dịch vụ tại nhà: lắp đặt hoặc đo đạc (đặc thù ngành nội thất).</summary>
public class BookingsController : ApiController
{
    public BookingsController(ISender mediator) : base(mediator) { }

    /// <summary>Đặt lịch lắp đặt / đo đạc tại nhà.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingCommand command)
    {
        var booking = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(booking, "Đã tiếp nhận lịch hẹn."));
    }

    /// <summary>Danh sách lịch hẹn theo số điện thoại khách.</summary>
    [HttpGet("by-phone")]
    public async Task<IActionResult> GetByPhone([FromQuery] string phone)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetBookingsByPhoneQuery(phone))));

    /// <summary>Cập nhật trạng thái lịch (admin): xác nhận / hoàn thành / hủy.</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,SuperAdmin,Operations")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusBody body)
    {
        var booking = await Mediator.Send(new UpdateBookingStatusCommand(id, body.NewStatus));
        return Ok(ApiResponse<object>.Ok(booking, "Đã cập nhật trạng thái lịch hẹn."));
    }
}

public record UpdateBookingStatusBody(ServiceAppointmentStatus NewStatus);
