using Harness.Modules.Shipping.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Shipping.Infrastructure.Persistence;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments", "shipping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TrackingCode).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.TrackingCode).IsUnique();
        builder.HasIndex(x => x.OrderId);
        builder.Property(x => x.ShippingFee).HasPrecision(14, 0);
        builder.Ignore(x => x.DomainEvents);
    }
}
