using Harness.Modules.Promotion.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Promotion.Infrastructure.Persistence;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("vouchers", "promotion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Value).HasPrecision(14, 0);
        builder.Property(x => x.MinOrderAmount).HasPrecision(14, 0);
        builder.Property(x => x.MaxDiscountAmount).HasPrecision(14, 0);
        builder.Ignore(x => x.DomainEvents);
    }
}

public class FlashSaleConfiguration : IEntityTypeConfiguration<FlashSale>
{
    public void Configure(EntityTypeBuilder<FlashSale> builder)
    {
        builder.ToTable("flash_sales", "promotion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.StartAt, x.EndAt });
        builder.Ignore(x => x.DomainEvents);
    }
}

public class FlashSaleItemConfiguration : IEntityTypeConfiguration<FlashSaleItem>
{
    public void Configure(EntityTypeBuilder<FlashSaleItem> builder)
    {
        builder.ToTable("flash_sale_items", "promotion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SalePrice).HasPrecision(14, 0);
        builder.HasIndex(x => new { x.FlashSaleId, x.ProductId }).IsUnique();
        builder.Ignore(x => x.IsSoldOut);
        builder.Ignore(x => x.DomainEvents);
    }
}
