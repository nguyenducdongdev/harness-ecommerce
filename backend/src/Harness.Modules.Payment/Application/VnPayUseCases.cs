using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Payment.Domain;
using Harness.Modules.Payment.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Payment.Application;

public record CreateVnPayPaymentCommand(
    Guid OrderId,
    decimal Amount,
    string OrderInfo,
    string? ReturnUrl = null,
    string ClientIp = "127.0.0.1") : IRequest<VnPayPaymentInitDto>;

public class CreateVnPayPaymentCommandValidator : AbstractValidator<CreateVnPayPaymentCommand>
{
    public CreateVnPayPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Số tiền thanh toán phải lớn hơn 0.");
        RuleFor(x => x.OrderInfo).NotEmpty().MaximumLength(250);
    }
}

public class CreateVnPayPaymentCommandHandler : IRequestHandler<CreateVnPayPaymentCommand, VnPayPaymentInitDto>
{
    private readonly IHarnessDbContext _db;
    private readonly VnPayService _vnPay;
    private readonly VnPayOptions _options;

    public CreateVnPayPaymentCommandHandler(
        IHarnessDbContext db, VnPayService vnPay, IOptions<VnPayOptions> options)
    {
        _db = db;
        _vnPay = vnPay;
        _options = options.Value;
    }

    public async Task<VnPayPaymentInitDto> Handle(CreateVnPayPaymentCommand request, CancellationToken cancellationToken)
    {
        var transaction = PaymentTransaction.Create(request.OrderId, "vnpay", request.Amount);
        _db.Set<PaymentTransaction>().Add(transaction);
        await _db.SaveChangesAsync(cancellationToken);

        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? _options.ReturnUrl : request.ReturnUrl;
        var paymentUrl = _vnPay.BuildPaymentUrl(request.OrderId, request.Amount, request.OrderInfo, returnUrl, request.ClientIp);

        return new VnPayPaymentInitDto(
            TransactionId: transaction.Id,
            OrderId: request.OrderId,
            Amount: request.Amount,
            PaymentUrl: paymentUrl);
    }
}

public record VnPayPaymentInitDto(Guid TransactionId, Guid OrderId, decimal Amount, string PaymentUrl);
