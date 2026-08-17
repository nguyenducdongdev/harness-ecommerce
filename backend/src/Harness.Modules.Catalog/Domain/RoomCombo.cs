using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Catalog.Domain;

public enum RoomType { LivingRoom = 1, BedRoom = 2, DiningRoom = 3, HomeOffice = 4 }

/// <summary>
/// Combo phòng — gộp nhiều sản phẩm nội thất thành 1 không gian hoàn chỉnh theo đúng
/// cách khách hay mua phối cảnh (VD: sofa + bàn trà + kệ TV; giường + tủ áo + bàn trang điểm).
/// Giá gốc tính động từ từng sản phẩm; có thể đặt giá combo (DiscountedPrice) để thể hiện ưu đãi.
/// </summary>
public class RoomCombo : AuditableEntity<int>
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public RoomType RoomType { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    /// <summary>Giá trọn bộ (nếu đặt) — khi null thì tính bằng tổng giá sản phẩm thành phần.</summary>
    public decimal? DiscountedPrice { get; private set; }

    private readonly List<ProductComboItem> _items = new();
    public IReadOnlyCollection<ProductComboItem> Items => _items.AsReadOnly();

    private RoomCombo() { } // EF

    public static RoomCombo Create(
        string name, string slug, RoomType roomType, string? description, decimal? discountedPrice = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên combo là bắt buộc.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug là bắt buộc.", nameof(slug));
        if (discountedPrice is < 0) throw new ArgumentException("Giá combo không được âm.", nameof(discountedPrice));

        return new RoomCombo
        {
            Name = name.Trim(),
            Slug = slug,
            RoomType = roomType,
            Description = description,
            DiscountedPrice = discountedPrice
        };
    }

    public void AddItem(int productId, int quantity = 1)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng phải dương.", nameof(quantity));
        _items.Add(new ProductComboItem
        {
            ProductId = productId,
            Quantity = quantity,
            SortOrder = _items.Count + 1
        });
    }
}

public class ProductComboItem : Entity<int>
{
    public int ComboId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int SortOrder { get; set; }
}
