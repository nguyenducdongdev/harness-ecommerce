using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Loyalty.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Loyalty.Application;

public record EarnPointsCommand(Guid CustomerId, decimal OrderAmount) : IRequest<LoyaltyDto>;

public class EarnPointsCommandValidator : AbstractValidator<EarnPointsCommand>
{
    public EarnPointsCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderAmount).GreaterThan(0);
    }
}

public class EarnPointsCommandHandler : IRequestHandler<EarnPointsCommand, LoyaltyDto>
{
    private readonly IHarnessDbContext _db;

    public EarnPointsCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<LoyaltyDto> Handle(EarnPointsCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Set<LoyaltyAccount>()
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);

        if (account is null)
        {
            account = LoyaltyAccount.Open(request.CustomerId);
            _db.Set<LoyaltyAccount>().Add(account);
        }

        account.EarnFromOrder(request.OrderAmount);
        await _db.SaveChangesAsync(cancellationToken);
        return new LoyaltyDto(account.CustomerId, account.Points, account.Tier.ToString(), account.LifetimeSpend);
    }
}

public record RedeemPointsCommand(Guid CustomerId, int Points) : IRequest<LoyaltyDto>;

public class RedeemPointsCommandHandler : IRequestHandler<RedeemPointsCommand, LoyaltyDto>
{
    private readonly IHarnessDbContext _db;

    public RedeemPointsCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<LoyaltyDto> Handle(RedeemPointsCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Set<LoyaltyAccount>()
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Khách hàng chưa có tài khoản tích điểm.");

        account.Redeem(request.Points);
        await _db.SaveChangesAsync(cancellationToken);
        return new LoyaltyDto(account.CustomerId, account.Points, account.Tier.ToString(), account.LifetimeSpend);
    }
}

public record LoyaltyDto(Guid CustomerId, int Points, string Tier, decimal LifetimeSpend);

public record GetLoyaltyQuery(Guid CustomerId) : IRequest<LoyaltyDto?>;

public class GetLoyaltyQueryHandler : IRequestHandler<GetLoyaltyQuery, LoyaltyDto?>
{
    private readonly IHarnessDbContext _db;

    public GetLoyaltyQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<LoyaltyDto?> Handle(GetLoyaltyQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.Set<LoyaltyAccount>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);
        return account is null
            ? null
            : new LoyaltyDto(account.CustomerId, account.Points, account.Tier.ToString(), account.LifetimeSpend);
    }
}
