using Harness.Modules.Order.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Order.Infrastructure.Persistence;

public class ServiceAppointmentConfiguration : IEntityTypeConfiguration<ServiceAppointment>
{
    public void Configure(EntityTypeBuilder<ServiceAppointment> builder)
    {
        builder.ToTable("service_appointments", "orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerPhone).HasMaxLength(15).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReceiverName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasMaxLength(15).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AppointmentType).HasConversion<int>();
        builder.Property(x => x.TimeSlot).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => x.CustomerPhone);
        builder.HasIndex(x => x.OrderId);
        builder.Ignore(x => x.DomainEvents);
    }
}
