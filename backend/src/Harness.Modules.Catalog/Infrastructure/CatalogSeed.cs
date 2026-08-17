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
            CreateProduct("Bàn trà mặt đá Óc chó Sydney", 6, 2, 6_500_000, 5_900_000, 12,
                "Bàn trà mặt đá nhân tạo, chân gỗ óc chó — điểm nhấn phòng khách.", "Hiện đại", "Đá nhân tạo + gỗ óc chó"),
            CreateProduct("Tủ áo gỗ óc chó 3 cánh", 3, 4, 22_000_000, null, 36,
                "Tủ áo 3 cánh gỗ óc chó tự nhiên, nhiều khoang cất đồ linh hoạt.", "Tân cổ điển", "Gỗ óc chó"),
            CreateProduct("Bàn trang điểm thông minh Ava", 7, 1, 7_200_000, null, 12,
                "Bàn trang điểm tích hợp đèn LED + gương thông minh.", "Hiện đại", "MDF + đèn LED"),
        };

        foreach (var product in products)
        {
            AddSizeVariants(product);
            db.Set<Product>().Add(product);
        }

        await db.SaveChangesAsync();

        // Combo phòng — dùng sản phẩm vừa seed (chỉ khi chưa có combo nào)
        var slugToId = products.ToDictionary(p => p.Slug, p => p.Id);
        await SeedCombosAsync(db, slugToId);
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
                : product.Name.Contains("Bàn trà") ? new[] { (90, 50, 45), (110, 60, 45) }
                : product.Name.Contains("Bàn trang điểm") ? new[] { (90, 45, 140), (120, 50, 150) }
                : product.Name.Contains("Tủ áo") ? new[] { (150, 60, 220), (180, 60, 220) }
                : product.Name.Contains("Kệ TV") ? new[] { (160, 35, 40), (180, 35, 40), (200, 40, 40) }
                : product.Name.Contains("Ghế ăn") ? new[] { (45, 52, 88) }
                : product.Name.Contains("Sofa bed") ? new[] { (180, 95, 85), (220, 95, 85) }
                : new[] { (100, 100, 100) }
        };

        foreach (var (w, d, h) in variants)
            product.AddVariant(ProductVariant.Create(
                product.Id, $"{product.Sku}-{w}x{d}x{h}", $"{w}x{d}x{h}cm", w, d, h));
    }

    /// <summary>Seed combo phòng (sofa + bàn + kệ; giường + tủ + bàn trang điểm) từ sản phẩm đã seed.</summary>
    private static async Task SeedCombosAsync(IHarnessDbContext db, IReadOnlyDictionary<string, int> productBySlug)
    {
        if (await db.Set<RoomCombo>().AnyAsync()) return;

        var definitions = new[]
        {
            new
            {
                Name = "Combo Phòng khách hiện đại",
                RoomType = RoomType.LivingRoom,
                Description = "Sofa góc + bàn trà + kệ TV — không gian tiếp khách liền mạch, tiết kiệm.",
                Items = new[] { ("sofa-goc-da-cong-nghiep-milano", 1), ("ban-tra-mat-da-oc-cho-sydney", 1), ("ke-tv-treo-tuong-thong-minh-smart-living", 1) }
            },
            new
            {
                Name = "Combo Phòng ngủ ấm cúng",
                RoomType = RoomType.BedRoom,
                Description = "Giường + tủ áo + bàn trang điểm cho phòng ngủ trọn vẹn.",
                Items = new[] { ("giuong-ngu-go-oc-cho-queen", 1), ("tu-ao-go-oc-cho-3-canh", 1), ("ban-trang-diem-thong-minh-ava", 1) }
            }
        };

        foreach (var def in definitions)
        {
            var resolved = def.Items
                .Select(item => (id: productBySlug.GetValueOrDefault(item.Item1, 0), qty: item.Item2))
                .Where(x => x.id > 0)
                .ToList();
            if (resolved.Count == 0) continue;

            var combo = RoomCombo.Create(def.Name, SlugHelper.Generate(def.Name), def.RoomType, def.Description);
            foreach (var (id, qty) in resolved)
                combo.AddItem(id, qty);

            db.Set<RoomCombo>().Add(combo);
        }

        await db.SaveChangesAsync();
    }
}
