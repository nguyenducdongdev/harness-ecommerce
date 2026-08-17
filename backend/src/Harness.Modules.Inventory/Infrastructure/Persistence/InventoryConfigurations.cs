using Harness.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Inventory.Infrastructure.Persistence;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses", "inventory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();

        builder.HasData(
            new { Id = 1, Code = "HCM-01", Name = "Showroom Quận 1", Address = "123 Nguyễn Huệ, Q1, TP.HCM", IsShowroom = true, IsActive = true, Phone = "02812345678", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 2, Code = "HN-01", Name = "Showroom Cầu Giấy", Address = "45 Xuân Thủy, Cầu Giấy, Hà Nội", IsShowroom = true, IsActive = true, Phone = "02412345678", CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" },
            new { Id = 3, Code = "KHO-TONG", Name = "Kho tổng Bình Dương", Address = "Khu CN VSIP, Bình Dương", IsShowroom = false, IsActive = true, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), CreatedBy = "seed" });
    }
}

public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("stock_levels", "inventory");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.WarehouseId, x.VariantSku }).IsUnique();
        builder.Property(x => x.VariantSku).HasMaxLength(64).IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements", "inventory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VariantSku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.VariantSku);
        builder.HasIndex(x => new { x.WarehouseId, x.CreatedAt });
        builder.Ignore(x => x.DomainEvents);
    }
}
