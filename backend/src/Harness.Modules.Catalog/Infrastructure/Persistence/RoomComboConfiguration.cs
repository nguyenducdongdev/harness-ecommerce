using Harness.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Catalog.Infrastructure.Persistence;

public class RoomComboConfiguration : IEntityTypeConfiguration<RoomCombo>
{
    public void Configure(EntityTypeBuilder<RoomCombo> builder)
    {
        builder.ToTable("room_combos", "catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.RoomType).HasConversion<int>();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DiscountedPrice).HasPrecision(14, 0);
        builder.HasIndex(x => x.IsActive);

        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.ComboId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}

public class ProductComboItemConfiguration : IEntityTypeConfiguration<ProductComboItem>
{
    public void Configure(EntityTypeBuilder<ProductComboItem> builder)
    {
        builder.ToTable("room_combo_items", "catalog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).IsRequired();
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ComboId);
        builder.Ignore(x => x.DomainEvents);
    }
}
