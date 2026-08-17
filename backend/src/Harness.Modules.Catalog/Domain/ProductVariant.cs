using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Catalog.Domain;

/// <summary>Biến thể sản phẩm nội thất theo kích thước (rộng × sâu × cao) và màu sắc.</summary>
public class ProductVariant : AuditableEntity<int>
{
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public string Sku { get; private set; } = default!;
    /// <summary>Ví dụ: "220x95x85cm" (sofa 3 chỗ) hoặc "160x200cm" (giường).</summary>
    public string SizeName { get; private set; } = default!;
    public int WidthCm { get; private set; }
    public int DepthCm { get; private set; }
    public int HeightCm { get; private set; }
    public string? Color { get; private set; }

    /// <summary>Giá riêng của biến thể (nếu null → dùng giá sản phẩm).</summary>
    public decimal? PriceOverride { get; private set; }

    private ProductVariant() { } // EF

    public static ProductVariant Create(
        int productId, string sku, string sizeName,
        int widthCm, int depthCm, int heightCm,
        string? color = null, decimal? priceOverride = null)
    {
        if (widthCm <= 0 || depthCm <= 0 || heightCm <= 0)
            throw new ArgumentException("Kích thước nội thất phải lớn hơn 0.");

        return new ProductVariant
        {
            ProductId = productId,
            Sku = sku,
            SizeName = sizeName,
            WidthCm = widthCm,
            DepthCm = depthCm,
            HeightCm = heightCm,
            Color = color,
            PriceOverride = priceOverride
        };
    }

    /// <summary>Giá thực tế của biến thể (ưu tiên PriceOverride).</summary>
    public decimal GetEffectivePrice(decimal productPrice)
        => PriceOverride ?? productPrice;
}

/// <summary>Integration event publish ra RabbitMQ khi sản phẩm mới được tạo (cho Search indexer, sàn TMĐT...).</summary>
public sealed record ProductCreatedIntegrationEvent(int ProductId, string Slug, string Name) : IntegrationEvent
{
    public override string EventType => "catalog.product.created";
}

/// <summary>Integration event khi giá/thông tin sản phẩm thay đổi.</summary>
public sealed record ProductUpdatedIntegrationEvent(int ProductId, decimal Price, decimal? SalePrice) : IntegrationEvent
{
    public override string EventType => "catalog.product.updated";
}
