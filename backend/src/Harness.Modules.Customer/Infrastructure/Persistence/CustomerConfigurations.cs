using Harness.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Customer.Infrastructure.Persistence;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", "customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(15).IsRequired();
        builder.HasIndex(x => x.Phone).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.HasMany(x => x.Addresses).WithOne().HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}

public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("addresses", "customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(50);
        builder.Property(x => x.ReceiverName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(15).IsRequired();
        builder.Property(x => x.FullAddress).HasMaxLength(500).IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}
