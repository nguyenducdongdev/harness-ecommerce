using Harness.Modules.Loyalty.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Loyalty.Infrastructure.Persistence;

public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("loyalty_accounts", "customer");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CustomerId).IsUnique();
        builder.Property(x => x.LifetimeSpend).HasPrecision(16, 0);
        builder.Ignore(x => x.DomainEvents);
    }
}
