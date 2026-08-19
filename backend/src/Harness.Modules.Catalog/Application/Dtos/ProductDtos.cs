using Harness.Modules.Catalog.Domain;

namespace Harness.Modules.Catalog.Application.Dtos;

public record ProductVariantDto(
    int Id,
    string Sku,
    string SizeName,
    int WidthCm,
    int DepthCm,
    int HeightCm,
    string? Color,
    decimal? PriceOverride);

public record ProductDto(
    int Id,
    string Name,
    string Slug,
    string Sku,
    string? ShortDescription,
    string? Description,
    int CategoryId,
    string? CategoryName,
    string? CategorySlug,
    int BrandId,
    string? BrandName,
    decimal Price,
    decimal? SalePrice,
    int WarrantyMonths,
    bool IsActive,
    bool IsFeatured,
    int ViewCount,
    Dictionary<string, string> Attributes,
    List<string> ImageUrls,
    IReadOnlyList<ProductVariantDto> Variants,
    string? Model3dUrl = null)
{
    /// <summary>Giá hiển thị trên website (giá sale nếu có).</summary>
    public decimal DisplayPrice => SalePrice ?? Price;
    public int DiscountPercent => SalePrice.HasValue && Price > 0
        ? (int)Math.Round((Price - SalePrice.Value) / Price * 100)
        : 0;
}

public record CategoryDto(int Id, string Name, string Slug, int? ParentId, int SortOrder, bool IsActive);

public record BrandDto(int Id, string Name, string Slug, string? OriginCountry);

/// <summary>Dữ liệu lọc thuộc tính cho sidebar (phong-cach, chat-lieu).</summary>
public record AttributeFilterDto(List<string> PhongCach, List<string> ChatLieu);

public sealed class ProductSort
{
    public const string Newest = "newest";
    public const string PriceAsc = "price-asc";
    public const string PriceDesc = "price-desc";
    public const string BestSelling = "best-selling";
    public const string Popular = "popular";
}
