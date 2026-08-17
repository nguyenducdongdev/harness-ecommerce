using Harness.Modules.Review.Domain;
using ReviewEntity = Harness.Modules.Review.Domain.Review;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Review.Infrastructure.Persistence;

public class ReviewConfiguration : IEntityTypeConfiguration<ReviewEntity>
{
    public void Configure(EntityTypeBuilder<ReviewEntity> builder)
    {
        builder.ToTable("reviews", "review");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(15).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ImageUrls).HasColumnType("jsonb");
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.ProductId, x.Status });
        builder.Ignore(x => x.DomainEvents);
    }
}
