using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Loyalty.Domain;

public enum MemberTier { Silver = 1, Gold = 2, Platinum = 3, Diamond = 4 }

public enum LoyaltyTransactionType { Earn = 1, Redeem = 2 }

/// <summary>Tài khoản tích điểm của khách — điểm dùng chung online + tại showroom.</summary>
public class LoyaltyAccount : AuditableEntity<Guid>
{
    /// <summary>Id trùng CustomerId ở module Customer (dùng chung định danh).</summary>
    public Guid CustomerId { get; private set; }
    public int Points { get; private set; }
    public decimal LifetimeSpend { get; private set; }
    public MemberTier Tier { get; private set; } = MemberTier.Silver;

    private readonly List<LoyaltyTransaction> _transactions = new();
    public IReadOnlyCollection<LoyaltyTransaction> Transactions => _transactions.AsReadOnly();

    private LoyaltyAccount() { } // EF

    public static LoyaltyAccount Open(Guid customerId) =>
        new() { Id = Guid.NewGuid(), CustomerId = customerId };

    /// <summary>Quy tắc: 10.000đ chi tiêu = 1 điểm. Tự động nâng hạng.</summary>
    public void EarnFromOrder(decimal orderAmount, string? orderNumber = null)
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

        _transactions.Add(LoyaltyTransaction.Create(
            Id, earned, LoyaltyTransactionType.Earn,
            string.IsNullOrWhiteSpace(orderNumber) ? "Tích điểm đơn hàng" : $"Đơn {orderNumber}",
            $"Tích 10.000đ = 1 điểm — đơn {orderNumber?.ToString() ?? "(không mã)"}"));
    }

    /// <summary>Đổi điểm lấy quà từ kho quà (Reward).</summary>
    public void RedeemReward(Reward reward, string? note = null)
    {
        if (!reward.IsActive) throw new InvalidOperationException($"Quà '{reward.Name}' hiện không hoạt động.");
        Redeem(reward.PointsCost, $"Đổi quà: {reward.Name}", note ?? $"Giá trị quà {reward.Value:N0}đ");
    }

    /// <summary>Trừ điểm (điểm đổi).</summary>
    public void Redeem(int points, string reference, string? note = null)
    {
        if (points <= 0) throw new ArgumentException("Số điểm đổi phải lớn hơn 0.");
        if (Points < points) throw new InvalidOperationException($"Không đủ điểm (hiện có {Points}).");
        Points -= points;
        _transactions.Add(LoyaltyTransaction.Create(Id, -points, LoyaltyTransactionType.Redeem, reference, note));
    }
}

/// <summary>Quà trong chương trình tích điểm ('đổi quà').</summary>
public class Reward : AuditableEntity<int>
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    /// <summary>Số điểm cần để đổi.</summary>
    public int PointsCost { get; private set; }
    /// <summary>Giá trị quy đổi (VND) để hiển thị.</summary>
    public decimal Value { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Reward() { } // EF

    public static Reward Create(string name, int pointsCost, decimal value, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên quà không được để trống.", nameof(name));
        if (pointsCost <= 0) throw new ArgumentException("Số điểm phải lớn hơn 0.", nameof(pointsCost));
        if (value <= 0) throw new ArgumentException("Giá trị quà phải lớn hơn 0.", nameof(value));

        return new Reward
        {
            Name = name.Trim(),
            Description = description,
            PointsCost = pointsCost,
            Value = value
        };
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>Sổ giao dịch điểm: mỗi lần cộng/trừ điểm đều được ghi lại.</summary>
public class LoyaltyTransaction : Entity<Guid>
{
    public Guid LoyaltyAccountId { get; private set; }
    /// <summary>Dương: cộng điểm. Âm: trừ điểm.</summary>
    public int PointsDelta { get; private set; }
    public LoyaltyTransactionType Type { get; private set; }
    public string Reference { get; private set; } = default!;
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private LoyaltyTransaction() { } // EF

    public static LoyaltyTransaction Create(
        Guid accountId, int pointsDelta, LoyaltyTransactionType type, string reference, string? note = null)
    {
        return new LoyaltyTransaction
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = accountId,
            PointsDelta = pointsDelta,
            Type = type,
            Reference = reference,
            Note = note,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
