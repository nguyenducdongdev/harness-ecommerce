using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Loyalty.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Loyalty.Application;

public record EarnPointsCommand(Guid CustomerId, decimal OrderAmount, string? OrderNumber = null) : IRequest<LoyaltyDto>;

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
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);

        if (account is null)
        {
            account = LoyaltyAccount.Open(request.CustomerId);
            _db.Set<LoyaltyAccount>().Add(account);
        }

        account.EarnFromOrder(request.OrderAmount, request.OrderNumber);
        await _db.SaveChangesAsync(cancellationToken);
        return LoyaltyMapper.ToDto(account);
    }
}

/// <summary>Đổi điểm lấy quà từ kho quà (Reward).</summary>
public record RedeemRewardCommand(Guid CustomerId, int RewardId) : IRequest<LoyaltyDto>;

public class RedeemRewardCommandValidator : AbstractValidator<RedeemRewardCommand>
{
    public RedeemRewardCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.RewardId).GreaterThan(0);
    }
}

public class RedeemRewardCommandHandler : IRequestHandler<RedeemRewardCommand, LoyaltyDto>
{
    private readonly IHarnessDbContext _db;

    public RedeemRewardCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<LoyaltyDto> Handle(RedeemRewardCommand request, CancellationToken cancellationToken)
    {
        var reward = await _db.Set<Reward>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RewardId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy quà đổi thưởng.");

        var account = await _db.Set<LoyaltyAccount>()
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Bạn chưa có tài khoản tích điểm.");

        account.RedeemReward(reward);
        await _db.SaveChangesAsync(cancellationToken);
        return LoyaltyMapper.ToDto(account);
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
        return account is null ? null : LoyaltyMapper.ToDto(account);
    }
}

// ===== Kho quà =====

public record GetRewardsQuery : IRequest<IReadOnlyList<RewardDto>>;

public class GetRewardsQueryHandler : IRequestHandler<GetRewardsQuery, IReadOnlyList<RewardDto>>
{
    private readonly IHarnessDbContext _db;

    public GetRewardsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<RewardDto>> Handle(GetRewardsQuery request, CancellationToken cancellationToken)
        => await _db.Set<Reward>().AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.PointsCost)
            .Select(r => new RewardDto(r.Id, r.Name, r.Description, r.PointsCost, r.Value))
            .ToListAsync(cancellationToken);
}

public record RewardDto(int Id, string Name, string? Description, int PointsCost, decimal Value);

// ===== Sổ giao dịch điểm =====

public record GetPointTransactionsQuery(Guid CustomerId) : IRequest<IReadOnlyList<LoyaltyTransactionDto>>;

public class GetPointTransactionsQueryHandler : IRequestHandler<GetPointTransactionsQuery, IReadOnlyList<LoyaltyTransactionDto>>
{
    private readonly IHarnessDbContext _db;

    public GetPointTransactionsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoyaltyTransactionDto>> Handle(
        GetPointTransactionsQuery request, CancellationToken cancellationToken)
        => await _db.Set<LoyaltyAccount>().AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId)
            .SelectMany(a => a.Transactions)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new LoyaltyTransactionDto(
                t.Id, t.PointsDelta, t.Type.ToString(), t.Reference, t.Note, t.CreatedAt))
            .ToListAsync(cancellationToken);
}

public record LoyaltyTransactionDto(
    Guid Id, int PointsDelta, string Type, string Reference, string? Note, DateTimeOffset CreatedAt);

internal static class LoyaltyMapper
{
    public static LoyaltyDto ToDto(LoyaltyAccount a) =>
        new(a.CustomerId, a.Points, a.Tier.ToString(), a.LifetimeSpend);
}
