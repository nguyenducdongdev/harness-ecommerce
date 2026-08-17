using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Integration.Infrastructure.Persistence;

public class IntegrationSyncLogConfiguration : IEntityTypeConfiguration<IntegrationSyncLog>
{
    public void Configure(EntityTypeBuilder<IntegrationSyncLog> builder)
    {
        builder.ToTable("integration_sync_logs", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetSystem).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Direction).HasMaxLength(5).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb");
        builder.Property(x => x.Error).HasMaxLength(500);
        builder.HasIndex(x => new { x.TargetSystem, x.CreatedAt });
        builder.Ignore(x => x.DomainEvents);
    }
}

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("event_outbox", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
        builder.Ignore(x => x.DomainEvents);
    }
}
