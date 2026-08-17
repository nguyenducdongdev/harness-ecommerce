using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Payment.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Payment.Presentation;

public class PaymentsController : ApiController
{
    public PaymentsController(ISender mediator) : base(mediator) { }

    /// <summary>Webhook nhận kết quả thanh toán từ cổng (VNPay/MoMo/ZaloPay). Phase 1: stub — Phase 3 ký HMAC.</summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest request)
    {
        await Mediator.Send(new RecordPaymentResultCommand(
            request.OrderId, request.Provider, request.Success, request.ProviderRef, request.RawPayload));
        return Ok(ApiResponse.Ok("Đã ghi nhận kết quả thanh toán."));
    }

    /// <summary>Lịch sử thanh toán của đơn.</summary>
    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetPaymentsByOrderQuery(orderId))));
}

public record PaymentWebhookRequest(Guid OrderId, string Provider, bool Success, string? ProviderRef, string? RawPayload);
