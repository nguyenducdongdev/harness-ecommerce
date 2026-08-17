using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Payment.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Payment.Application;

/// <summary>Nhận kết quả từ webhook cổng thanh toán (VNPay/MoMo/ZaloPay).</summary>
public record RecordPaymentResultCommand(Guid OrderId, string Provider, bool Success, string? ProviderRef, string? RawPayload)
    : IRequest<PaymentDto>;

public class RecordPaymentResultCommandValidator : AbstractValidator<RecordPaymentResultCommand>
{
    public RecordPaymentResultCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(20);
    }
}

public class RecordPaymentResultCommandHandler : IRequestHandler<RecordPaymentResultCommand, PaymentDto>
{
    private readonly IHarnessDbContext _db;

    public RecordPaymentResultCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<PaymentDto> Handle(RecordPaymentResultCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _db.Set<PaymentTransaction>()
            .FirstOrDefaultAsync(t => t.OrderId == request.OrderId && t.Provider == request.Provider, cancellationToken);

        if (transaction is null)
        {
            transaction = PaymentTransaction.Create(request.OrderId, request.Provider, 0);
            _db.Set<PaymentTransaction>().Add(transaction);
        }

        if (request.Success)
        {
            transaction.MarkSucceeded(request.ProviderRef, request.RawPayload);
            _db.AddToOutbox(new PaymentSucceededIntegrationEvent(request.OrderId, request.Provider, transaction.Amount));
        }
        else
        {
            transaction.MarkFailed(request.RawPayload);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new PaymentDto(transaction.Id, transaction.OrderId, transaction.Provider, transaction.Status.ToString());
    }
}

public record PaymentDto(Guid Id, Guid OrderId, string Provider, string Status);

public record GetPaymentsByOrderQuery(Guid OrderId) : IRequest<IReadOnlyList<PaymentDto>>;

public class GetPaymentsByOrderQueryHandler : IRequestHandler<GetPaymentsByOrderQuery, IReadOnlyList<PaymentDto>>
{
    private readonly IHarnessDbContext _db;

    public GetPaymentsByOrderQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<PaymentDto>> Handle(GetPaymentsByOrderQuery request, CancellationToken cancellationToken)
        => await _db.Set<PaymentTransaction>().AsNoTracking()
            .Where(t => t.OrderId == request.OrderId)
            .Select(t => new PaymentDto(t.Id, t.OrderId, t.Provider, t.Status.ToString()))
            .ToListAsync(cancellationToken);
}
