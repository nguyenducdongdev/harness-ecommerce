using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Integration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Integration.Infrastructure.Persistence;

public class ErpSalesOrderConfiguration : IEntityTypeConfiguration<ErpSalesOrder>
{
    public void Configure(EntityTypeBuilder<ErpSalesOrder> builder)
    {
        builder.ToTable("erp_sales_orders", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ErpOrderNo).HasMaxLength(40).IsRequired();
        builder.Property(x => x.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DeliveryMethod).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.OrderNumber);
        builder.Ignore(x => x.DomainEvents);
    }
}

public class ErpSyncRecordConfiguration : IEntityTypeConfiguration<ErpSyncRecord>
{
    public void Configure(EntityTypeBuilder<ErpSyncRecord> builder)
    {
        builder.ToTable("erp_sync_records", "integration");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.TargetSystem).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb");
        builder.Property(x => x.Error).HasMaxLength(500);
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.Ignore(x => x.DomainEvents);
    }
}