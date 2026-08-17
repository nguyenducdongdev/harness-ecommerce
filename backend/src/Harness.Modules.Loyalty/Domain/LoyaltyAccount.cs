using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Loyalty.Domain;

public enum MemberTier { Silver = 1, Gold = 2, Platinum = 3, Diamond = 4 }

/// <summary>Tài khoản tích điểm của khách — điểm dùng chung online + tại showroom.</summary>
public class LoyaltyAccount : AuditableEntity<Guid>
{
    /// <summary>Id trùng CustomerId ở module Customer (dùng chung định danh).</summary>
    public Guid CustomerId { get; private set; }
    public int Points { get; private set; }
    public decimal LifetimeSpend { get; private set; }
    public MemberTier Tier { get; private set; } = MemberTier.Silver;

    private LoyaltyAccount() { } // EF

    public static LoyaltyAccount Open(Guid customerId) =>
        new() { Id = Guid.NewGuid(), CustomerId = customerId };

    /// <summary>Quy tắc: 10.000đ chi tiêu = 1 điểm. Tự động nâng hạng.</summary>
    public void EarnFromOrder(decimal orderAmount)
    {
        if (orderAmount <= 0) throw new ArgumentException("Số tiền đơn phải lớn hơn 0.");
        var earned = (int)(orderAmount / 10_000m);
        Points += earned;
        LifetimeSpend += orderAmount;
        Tier = LifetimeSpend switch
        {
            >= 100_000_000 => MemberTier.Diamond,
            >= 50_000_000 => MemberTier.Platinum,
            >= 20_000_000 => MemberTier.Gold,
            _ => MemberTier.Silver
        };
    }

    public void Redeem(int points)
    {
        if (points <= 0) throw new ArgumentException("Số điểm đổi phải lớn hơn 0.");
        if (Points < points) throw new InvalidOperationException($"Không đủ điểm (hiện có {Points}).");
        Points -= points;
    }
}
