using Harness.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Harness.Modules.Catalog.Infrastructure.Persistence;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", "catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);

        // Seed danh mục nội thất ứng dụng
        builder.HasData(
            new { Id = 1, Name = "Sofa & Ghế thư giãn", Slug = "sofa", SortOrder = 1, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 2, Name = "Giường & Phòng ngủ", Slug = "giuong-phong-ngu", SortOrder = 2, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 3, Name = "Tủ & Kệ", Slug = "tu-ke", SortOrder = 3, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 4, Name = "Bàn ăn & Ghế ăn", Slug = "ban-an", SortOrder = 4, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 5, Name = "Nội thất văn phòng", Slug = "van-phong", SortOrder = 5, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 6, Name = "Nội thất phòng khách", Slug = "phong-khach", SortOrder = 6, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 7, Name = "Nội thất thông minh", Slug = "noi-that-thong-minh", SortOrder = 7, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 8, Name = "Phụ kiện & Trang trí", Slug = "phu-kien-trang-tri", SortOrder = 8, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" });
    }
}

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands", "catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(170).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasData(
            new { Id = 1, Name = "Nội Thất Việt", Slug = "noi-that-viet", OriginCountry = "Việt Nam", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 2, Name = "Nhà Xinh", Slug = "nha-xinh", OriginCountry = "Việt Nam", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 3, Name = "An Cường", Slug = "an-cuong", OriginCountry = "Việt Nam", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 4, Name = "Xưởng Mộc Việt", Slug = "xuong-moc-viet", OriginCountry = "Việt Nam", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" });
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.Price).HasPrecision(14, 0);
        builder.Property(x => x.SalePrice).HasPrecision(14, 0);

        // Thuộc tính động JSONB — linh hoạt cho mọi loại sản phẩm nội thất
        builder.Property(x => x.Attributes)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
        builder.Property(x => x.ImageUrls)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CategoryId, x.IsActive });
        builder.HasIndex(x => x.IsFeatured).HasFilter("is_featured = true");
        builder.Ignore(x => x.DomainEvents);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants", "catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.SizeName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(50);
        builder.Property(x => x.PriceOverride).HasPrecision(14, 0);

        builder.HasOne(x => x.Product).WithMany(p => p.Variants)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}
