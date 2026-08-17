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
    public string Name { get; private set; } = default!;
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<FlashSaleItem> _items = new();
    public IReadOnlyCollection<FlashSaleItem> Items => _items.AsReadOnly();

    private FlashSale() { } // EF

    public static FlashSale Create(string name, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên flash sale không được để trống.", nameof(name));
        if (endAt <= startAt) throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu.", nameof(endAt));

        return new FlashSale
        {
            Name = name.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            IsActive = true
        };
    }

    public bool IsActiveNow(DateTimeOffset now) => IsActive && now >= StartAt && now <= EndAt;

    public void Deactivate() => IsActive = false;

    public FlashSaleItem AddItem(int productId, decimal salePrice, int quantityLimit)
    {
        if (productId <= 0) throw new ArgumentException("ProductId phải lớn hơn 0.", nameof(productId));
        if (salePrice <= 0) throw new ArgumentException("Giá flash sale phải lớn hơn 0.", nameof(salePrice));
        if (quantityLimit <= 0) throw new ArgumentException("Số lượng giới hạn phải lớn hơn 0.", nameof(quantityLimit));
        if (_items.Any(i => i.ProductId == productId))
            throw new InvalidOperationException($"Sản phẩm {productId} đã có trong flash sale này.");

        var item = FlashSaleItem.Create(Id, productId, salePrice, quantityLimit);
        _items.Add(item);
        return item;
    }
}

public class FlashSaleItem : Entity<int>
{
    public int FlashSaleId { get; private set; }
    public int ProductId { get; private set; }
    public decimal SalePrice { get; private set; }
    public int QuantityLimit { get; private set; }
    public int QuantitySold { get; private set; }
    public bool IsSoldOut => QuantitySold >= QuantityLimit;

    private FlashSaleItem() { } // EF

    public static FlashSaleItem Create(int flashSaleId, int productId, decimal salePrice, int quantityLimit)
    {
        return new FlashSaleItem
        {
            FlashSaleId = flashSaleId,
            ProductId = productId,
            SalePrice = salePrice,
            QuantityLimit = quantityLimit,
            QuantitySold = 0
        };
    }

    public void IncreaseSold(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng bán phải lớn hơn 0.", nameof(quantity));
        if (QuantitySold + quantity > QuantityLimit)
            throw new InvalidOperationException("Vượt quá số lượng flash sale cho phép.");
        QuantitySold += quantity;
    }
}
