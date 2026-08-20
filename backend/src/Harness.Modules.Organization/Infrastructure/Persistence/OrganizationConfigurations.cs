using Harness.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Organization.Infrastructure.Persistence;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores", "organization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.ManagerName).HasMaxLength(100);

        builder.HasData(
            Store.Create("CH-Q1", "Showroom Quận 1", "123 Nguyễn Huệ, Quận 1, TP.HCM", "02812345678", "Nguyễn Văn Quản Lý"),
            Store.Create("CH-CG", "Showroom Cầu Giấy", "45 Xuân Thủy, Cầu Giấy, Hà Nội", "02412345678", "Trần Thị Cửa Hàng Trưởng")
        );
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", "organization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StaffName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StoreName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.StaffId, x.WorkDate }).IsUnique();
    }
}

public class KpiTargetConfiguration : IEntityTypeConfiguration<KpiTarget>
{
    public void Configure(EntityTypeBuilder<KpiTarget> builder)
    {
        builder.ToTable("kpi_targets", "organization");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StaffName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StoreName).HasMaxLength(200);
        builder.Property(x => x.TargetRevenue).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.StaffId, x.Month, x.Year }).IsUnique();
    }
}
