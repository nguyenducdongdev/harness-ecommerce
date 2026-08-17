using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Promotion.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Promotion.Application;

public record ValidateVoucherQuery(string Code, decimal OrderAmount) : IRequest<VoucherResultDto>;

public record VoucherResultDto(bool IsValid, decimal DiscountAmount, string? Message);

public class ValidateVoucherQueryHandler : IRequestHandler<ValidateVoucherQuery, VoucherResultDto>
{
    private readonly IHarnessDbContext _db;

    public ValidateVoucherQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<VoucherResultDto> Handle(ValidateVoucherQuery request, CancellationToken cancellationToken)
    {
        var voucher = await _db.Set<Voucher>().AsNoTracking()
            .FirstOrDefaultAsync(v => v.Code == request.Code.ToUpperInvariant(), cancellationToken);

        if (voucher is null)
            return new VoucherResultDto(false, 0, "Mã giảm giá không tồn tại.");

        try
        {
            var discount = voucher.CalculateDiscount(request.OrderAmount, DateTimeOffset.UtcNow);
            return new VoucherResultDto(true, discount, $"Áp dụng thành công: giảm {discount:N0}đ.");
        }
        catch (InvalidOperationException ex)
        {
            return new VoucherResultDto(false, 0, ex.Message);
        }
    }
}

public record CreateVoucherCommand(
    string Code, DiscountType Type, decimal Value,
    DateTimeOffset StartAt, DateTimeOffset EndAt,
    decimal MinOrderAmount = 0, decimal? MaxDiscountAmount = null, int? UsageLimit = null) : IRequest<int>;

public class CreateVoucherCommandValidator : AbstractValidator<CreateVoucherCommand>
{
    public CreateVoucherCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Value).GreaterThan(0);
    }
}

public class CreateVoucherCommandHandler : IRequestHandler<CreateVoucherCommand, int>
{
    private readonly IHarnessDbContext _db;

    public CreateVoucherCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<int> Handle(CreateVoucherCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Set<Voucher>().AnyAsync(v => v.Code == request.Code.ToUpperInvariant(), cancellationToken);
        if (exists) throw new InvalidOperationException($"Voucher '{request.Code}' đã tồn tại.");

        var voucher = Voucher.Create(request.Code, request.Type, request.Value,
            request.StartAt, request.EndAt, request.MinOrderAmount, request.MaxDiscountAmount, request.UsageLimit);

        _db.Set<Voucher>().Add(voucher);
        await _db.SaveChangesAsync(cancellationToken);
        return voucher.Id;
    }
}
