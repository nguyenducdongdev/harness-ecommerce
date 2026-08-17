using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Payment.Application;
using Harness.Modules.Payment.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Payment.Presentation;

public class PaymentsController : ApiController
{
    private readonly VnPayService _vnPay;
    private readonly VnPayOptions _vnPayOptions;

    public PaymentsController(ISender mediator, VnPayService vnPay, IOptions<VnPayOptions> vnPayOptions) : base(mediator)
    {
        _vnPay = vnPay;
        _vnPayOptions = vnPayOptions.Value;
    }

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

    // ===== VNPay sandbox =====

    /// <summary>Khởi tạo thanh toán VNPay → trả về URL redirect tới cổng sandbox.</summary>
    [HttpPost("vnpay/create")]
    public async Task<IActionResult> VnPayCreate([FromBody] CreateVnPayPaymentCommand command)
        => Ok(ApiResponse.Ok(await Mediator.Send(command), "Đã tạo URL thanh toán VNPay."));

    /// <summary>IPN — VNPay gọi lại (server-to-server) để xác nhận kết quả giao dịch.</summary>
    [HttpGet("vnpay/ipn")]
    [Produces("text/plain")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var readOnly = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        var verify = VerifySignatureFromQuery(readOnly);
        if (verify is null)
            return Content("RspCode=97&Message=Invalid Signature", "text/plain");

        if (verify.Success)
        {
            await Mediator.Send(verify.RecordCommand!, cancellationToken);
            return Content("RspCode=00&Message=Confirm Success", "text/plain");
        }

        return Content("RspCode=01&Message=Transaction Failed", "text/plain");
    }

    /// <summary>Return URL — trình duyệt khách được VNPay điều hướng về sau khi thanh toán.</summary>
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var readOnly = Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        var verify = VerifySignatureFromQuery(readOnly);
        var baseUrl = _vnPayOptions.ReturnUrl;

        if (verify is { Success: true })
        {
            await Mediator.Send(verify.RecordCommand!, cancellationToken);
            return Redirect($"{baseUrl}?payment=success");
        }

        return Redirect($"{baseUrl}?payment=failed");
    }

    private VnPayVerification? VerifySignatureFromQuery(IReadOnlyDictionary<string, string> parameters)
    {
        parameters.TryGetValue("vnp_SecureHash", out var secureHash);
        var toVerify = parameters
            .Where(kv => kv.Key is not ("vnp_SecureHash" or "vnp_SecureHashType"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (!_vnPay.VerifySignature(toVerify, secureHash))
            return null;

        var success = parameters.TryGetValue("vnp_ResponseCode", out var code) && code == "00";
        var orderId = Guid.Empty;
        if (parameters.TryGetValue("vnp_TxnRef", out var txnRef))
            Guid.TryParseExact(txnRef, "N", out orderId);

        if (orderId == Guid.Empty) return new VnPayVerification(false, null);

        var command = new RecordPaymentResultCommand(
            orderId, "vnpay", success, parameters.GetValueOrDefault("vnp_TransactionNo"), null);
        return new VnPayVerification(success, command);
    }

    private sealed record VnPayVerification(bool Success, RecordPaymentResultCommand? RecordCommand);
}

public record PaymentWebhookRequest(Guid OrderId, string Provider, bool Success, string? ProviderRef, string? RawPayload);
