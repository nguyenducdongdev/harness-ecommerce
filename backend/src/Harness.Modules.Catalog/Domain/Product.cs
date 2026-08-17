using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Catalog.Domain;

/// <summary>
/// Sản phẩm nội thất ứng dụng (sofa, giường, tủ, bàn ghế, nội thất thông minh...).
/// Thuộc tính chuyên ngành linh hoạt lưu JSONB (chất liệu, phong cách, số ghế, xuất xứ...).
/// </summary>
public class Product : AuditableEntity<int>
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public string? ShortDescription { get; private set; }
    public string? Description { get; private set; }

    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = default!;
    public int BrandId { get; private set; }
    public Brand Brand { get; private set; } = default!;

    /// <summary>Giá cơ bản (VND), áp dụng cho biến thể mặc định.</summary>
    public decimal Price { get; private set; }
    /// <summary>Giá khuyến mãi — phải nhỏ hơn Price.</summary>
    public decimal? SalePrice { get; private set; }

    public int WarrantyMonths { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public int ViewCount { get; private set; }

    /// <summary>Thuộc tính động: { "phongCach": "Hiện đại", "chatLieu": "Gỗ óc chó", "soGhe": "3 chỗ", ... }</summary>
    public Dictionary<string, string> Attributes { get; private set; } = new();
    public List<string> ImageUrls { get; private set; } = new();

    private readonly List<ProductVariant> _variants = new();
    public ICollection<ProductVariant> Variants => _variants;

    private Product() { } // EF

    public static Product Create(
        string name, string slug, string sku, int categoryId, int brandId,
        decimal price, decimal? salePrice, int warrantyMonths,
        string? shortDescription, string? description,
        Dictionary<string, string>? attributes = null, List<string>? imageUrls = null,
        bool isFeatured = false)
    {
        if (price <= 0)
            throw new ArgumentException("Giá bán phải lớn hơn 0.", nameof(price));
        if (salePrice.HasValue && salePrice.Value >= price)
            throw new ArgumentException("Giá khuyến mãi phải nhỏ hơn giá gốc.", nameof(salePrice));

        var product = new Product
        {
            Name = name,
            Slug = slug,
            Sku = sku,
            CategoryId = categoryId,
            BrandId = brandId,
            Price = price,
            SalePrice = salePrice,
            WarrantyMonths = warrantyMonths,
            ShortDescription = shortDescription,
            Description = description,
            Attributes = attributes ?? new Dictionary<string, string>(),
            ImageUrls = imageUrls ?? new List<string>(),
            IsFeatured = isFeatured
        };

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Slug));
        return product;
    }

    public void UpdatePrice(decimal price, decimal? salePrice)
    {
        if (price <= 0) throw new ArgumentException("Giá bán phải lớn hơn 0.");
        if (salePrice.HasValue && salePrice.Value >= price)
            throw new ArgumentException("Giá khuyến mãi phải nhỏ hơn giá gốc.");
        Price = price;
        SalePrice = salePrice;
    }

    public void AddVariant(ProductVariant variant)
    {
        if (_variants.Any(v => v.Sku == variant.Sku))
            throw new InvalidOperationException($"Biến thể SKU '{variant.Sku}' đã tồn tại.");
        _variants.Add(variant);
    }

    public void Deactivate() => IsActive = false;
    public void IncreaseViewCount() => ViewCount++;
}

public sealed record ProductCreatedDomainEvent(string Slug) : DomainEvent;
