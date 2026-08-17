using OrderEntity = Harness.Modules.Order.Domain.Order;
using Harness.Modules.Order.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Order.Infrastructure.Persistence;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("orders", "orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.CustomerPhone);
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(15).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(200);
        builder.Property(x => x.ShippingAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.ItemsTotal).HasPrecision(14, 0);
        builder.Property(x => x.ShippingFee).HasPrecision(14, 0);
        builder.Property(x => x.DiscountAmount).HasPrecision(14, 0);
        builder.Ignore(x => x.TotalAmount); // computed property, không map
        builder.Ignore(x => x.DomainEvents);

        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", "orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VariantSku).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(14, 0);
        builder.Ignore(x => x.LineTotal);
        builder.Ignore(x => x.DomainEvents);
    }
}
