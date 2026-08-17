using Harness.Modules.Catalog.Domain;

namespace Harness.Modules.Catalog.Application.Abstractions;

/// <summary>
/// Bản ghi sản phẩm trong chỉ mục Elasticsearch (full-text search).
/// Là hợp đồng cấp Application — implementation query/index thuộc Infrastructure.
/// </summary>
public class ProductSearchDocument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public double Price { get; set; }
    public double? SalePrice { get; set; }
    public double DisplayPrice => SalePrice ?? Price;
    public int WarrantyMonths { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public string[] Attributes { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[] ImageUrls { get; set; } = Array.Empty<string>();
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Chuyển sản phẩm + danh mục + thương hiệu thành document tìm kiếm.</summary>
    public static ProductSearchDocument FromProduct(
        Product p, string? categoryName, string? categorySlug, string? brandName)
    {
        var attributes = new List<string> { $"cat={categorySlug}", $"brand={brandName}" };
        attributes.AddRange(p.Attributes
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{kv.Key}:{kv.Value}"));

        return new ProductSearchDocument
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = categoryName ?? string.Empty,
            CategorySlug = categorySlug ?? string.Empty,
            BrandId = p.BrandId,
            BrandName = brandName ?? string.Empty,
            Price = (double)p.Price,
            SalePrice = (double?)p.SalePrice,
            WarrantyMonths = p.WarrantyMonths,
            IsActive = p.IsActive,
            IsFeatured = p.IsFeatured,
            Attributes = attributes.ToArray(),
            Tags = p.Sku.Split('-'),
            ImageUrls = p.ImageUrls.ToArray(),
            CreatedAt = p.CreatedAt
        };
    }
}
