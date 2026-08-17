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
        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(x => x.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.DomainEvents);
    }
}

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("loyalty_transactions", "customer");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.LoyaltyAccountId);
        builder.Property(x => x.Reference).HasMaxLength(200).IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}

public class RewardConfiguration : IEntityTypeConfiguration<Reward>
{
    public void Configure(EntityTypeBuilder<Reward> builder)
    {
        builder.ToTable("loyalty_rewards", "customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Value).HasPrecision(14, 0);
        builder.Ignore(x => x.DomainEvents);
    }
}

