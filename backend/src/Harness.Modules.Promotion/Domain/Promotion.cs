using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Promotion.Domain;

public enum DiscountType { Percent = 1, FixedAmount = 2 }

/// <summary>Mã giảm giá / voucher.</summary>
public class Voucher : AuditableEntity<int>
{
    public string Code { get; private set; } = default!;
    public DiscountType Type { get; private set; }
    /// <summary>Percent: 10 = 10%. FixedAmount: số VND.</summary>
    public decimal Value { get; private set; }
    public decimal MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Voucher() { } // EF

    public static Voucher Create(string code, DiscountType type, decimal value,
        DateTimeOffset startAt, DateTimeOffset endAt,
        decimal minOrderAmount = 0, decimal? maxDiscountAmount = null, int? usageLimit = null)
    {
        if (type == DiscountType.Percent && (value <= 0 || value > 100))
            throw new ArgumentException("Giảm theo % phải từ 1-100.");
        if (type == DiscountType.FixedAmount && value <= 0)
            throw new ArgumentException("Số tiền giảm phải lớn hơn 0.");
        if (endAt <= startAt) throw new ArgumentException("Thời gian kết thúc phải sau bắt đầu.");

        return new Voucher
        {
            Code = code.ToUpperInvariant(), Type = type, Value = value,
            StartAt = startAt, EndAt = endAt,
            MinOrderAmount = minOrderAmount, MaxDiscountAmount = maxDiscountAmount, UsageLimit = usageLimit
        };
    }

    /// <summary>Tính số tiền được giảm cho đơn hàng — ném exception nếu voucher không dùng được.</summary>
    public decimal CalculateDiscount(decimal orderAmount, DateTimeOffset now)
    {
        if (!IsActive) throw new InvalidOperationException("Voucher đã bị vô hiệu hóa.");
        if (now < StartAt || now > EndAt) throw new InvalidOperationException("Voucher ngoài thời gian hiệu lực.");
        if (UsageLimit.HasValue && UsedCount >= UsageLimit) throw new InvalidOperationException("Voucher đã hết lượt dùng.");
        if (orderAmount < MinOrderAmount)
            throw new InvalidOperationException($"Đơn tối thiểu {MinOrderAmount:N0}đ mới dùng được voucher này.");

        var discount = Type == DiscountType.Percent
            ? Math.Round(orderAmount * Value / 100)
            : Value;

        if (MaxDiscountAmount.HasValue) discount = Math.Min(discount, MaxDiscountAmount.Value);
        return Math.Min(discount, orderAmount);
    }
}

/// <summary>Flash sale: sản phẩm giá ưu đãi giới hạn suất trong khung giờ.</summary>
public class FlashSale : AuditableEntity<int>
{
    public string Name { get; set; } = default!;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FlashSaleItem : Entity<int>
{
    public int FlashSaleId { get; set; }
    public int ProductId { get; set; }
    public decimal SalePrice { get; set; }
    public int QuantityLimit { get; set; }
    public int QuantitySold { get; set; }
    public bool IsSoldOut => QuantitySold >= QuantityLimit;
}
