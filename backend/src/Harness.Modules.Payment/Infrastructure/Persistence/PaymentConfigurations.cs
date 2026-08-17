using Harness.Modules.Payment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Payment.Infrastructure.Persistence;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions", "orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(14, 0);
        builder.Property(x => x.ProviderTransactionRef).HasMaxLength(100);
        builder.Property(x => x.RawPayload).HasColumnType("jsonb");
        builder.HasIndex(x => x.OrderId);
        builder.Ignore(x => x.DomainEvents);
    }
}
