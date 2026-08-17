using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Domain;

namespace Harness.Modules.Catalog.Infrastructure;

/// <summary>
/// Seed sản phẩm nội thất mẫu — chạy khi bảng products trống, gọi từ Harness.Api khi khởi động (Development).
/// </summary>
public static class CatalogSeed
{
    public static async Task SeedAsync(IHarnessDbContext db)
    {
        if (await db.Set<Product>().AnyAsync()) return;

        var products = new List<Product>
        {
            CreateProduct("Sofa góc da công nghiệp Milano", 1, 1, 45_000_000, 38_500_000, 24,
                "Sofa góc thiết kế Ý, khung gỗ sồi đã tẩm sấy, đệm mút D35 cao cấp.", "Hiện đại", "Da công nghiệp"),
            CreateProduct("Giường ngủ gỗ óc chó Queen", 2, 4, 28_500_000, 24_900_000, 36,
                "Giường gỗ óc chó tự nhiên, đầu giường bọc nỉ cao cấp.", "Tân cổ điển", "Gỗ óc chó"),
            CreateProduct("Tủ bếp acrylic cao cấp An Cường", 3, 3, 42_000_000, null, 60,
                "Tủ bếp trên+dưới, mặt acrylic bóng gương, bản lề giảm chấn.", "Hiện đại", "Acrylic + MDF lõi xanh"),
            CreateProduct("Bàn ăn mặt đá 6 ghế Toronto", 4, 2, 18_900_000, 16_500_000, 12,
                "Bàn ăn mặt đá nhân tạo cao cấp, chân inox sơn tĩnh điện.", "Hiện đại", "Đá nhân tạo + inox"),
            CreateProduct("Bàn làm việc chân sắt Woodi", 5, 4, 4_200_000, 3_500_000, 12,
                "Mặt gỗ cao su ghép thanh, chân sắt sơn tĩnh điện chắc chắn.", "Scandinavian", "Gỗ cao su + sắt"),
            CreateProduct("Kệ TV treo tường thông minh Smart Living", 7, 1, 6_800_000, null, 12,
                "Kệ TV kết hợp hộc chứa đồ ẩn, tương thích thiết bị nhà thông minh.", "Hiện đại", "MDF phủ melamine"),
            CreateProduct("Ghế ăn bọc nỉ vàng đồng Lumière", 4, 2, 2_100_000, 1_750_000, 12,
                "Ghế ăn chân gỗ bech bọc nỉ mềm mại, kiểu dáng thanh lịch.", "Tân cổ điển", "Nỉ + gỗ bech"),
            CreateProduct("Sofa bed đa năng thông minh Flexi", 7, 1, 21_500_000, 18_900_000, 24,
                "Sofa kéo dài thành giường, hộc chứa đồ bên dưới — tiết kiệm diện tích.", "Hiện đại", "Vải nhung + khung thép"),
        };

        foreach (var product in products)
        {
            AddSizeVariants(product);
            db.Set<Product>().Add(product);
        }

        await db.SaveChangesAsync();
    }

    private static Product CreateProduct(
        string name, int categoryId, int brandId,
        decimal price, decimal? salePrice, int warrantyMonths,
        string? description, string? style, string? material) =>
        Product.Create(
            name, SlugHelper.Generate(name),
            $"NT-{categoryId:D2}{brandId:D2}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            categoryId, brandId, price, salePrice, warrantyMonths,
            description, $"{name}. {description ?? ""} Bảo hành chính hãng {warrantyMonths} tháng, miễn phí lắp đặt tận nhà.",
            attributes: new Dictionary<string, string?>
            {
                ["phong-cach"] = style,
                ["chat-lieu"] = material,
                ["xuat-xu"] = "Việt Nam"
            }.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value!),
            isFeatured: salePrice.HasValue);

    /// <summary>Biến thể kích thước phổ biến (rộng × sâu × cao — cm) cho từng dòng sản phẩm.</summary>
    private static void AddSizeVariants(Product product)
    {
        var variants = product.Name.Contains("Sofa góc") switch
        {
            true => new[] { (280, 190, 82), (320, 200, 82), (360, 210, 82) }, // sofa góc: dài × sâu × cao
            false => product.Name.Contains("Giường") ? new[] { (160, 200, 100), (180, 200, 100) }
                : product.Name.Contains("Tủ bếp") ? new[] { (280, 60, 220), (320, 60, 220), (360, 60, 220) }
                : product.Name.Contains("Bàn ăn") ? new[] { (140, 80, 75), (160, 80, 75), (180, 90, 75) }
                : product.Name.Contains("Bàn làm việc") ? new[] { (120, 60, 75), (140, 70, 75), (160, 70, 75) }
                : product.Name.Contains("Kệ TV") ? new[] { (160, 35, 40), (180, 35, 40), (200, 40, 40) }
                : product.Name.Contains("Ghế ăn") ? new[] { (45, 52, 88) }
                : product.Name.Contains("Sofa bed") ? new[] { (180, 95, 85), (220, 95, 85) }
                : new[] { (100, 100, 100) }
        };

        foreach (var (w, d, h) in variants)
            product.AddVariant(ProductVariant.Create(
                product.Id, $"{product.Sku}-{w}x{d}x{h}", $"{w}x{d}x{h}cm", w, d, h));
    }
}
