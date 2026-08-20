using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Organization.Domain;

/// <summary>
/// Chỉ tiêu KPI cho nhân viên Sales hoặc Cửa hàng theo tháng/năm.
/// </summary>
public class KpiTarget : AuditableEntity<Guid>
{
    public Guid StaffId { get; private set; }
    public string StaffName { get; private set; } = default!;
    public Guid? StoreId { get; private set; }
    public string? StoreName { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }
    public decimal TargetRevenue { get; private set; }
    public int TargetOrders { get; private set; }
    public string? Notes { get; private set; }

    private KpiTarget() { }

    public static KpiTarget Create(
        Guid staffId, string staffName, Guid? storeId, string? storeName,
        int month, int year, decimal targetRevenue, int targetOrders, string? notes = null)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month), "Tháng phải từ 1 đến 12.");
        if (year < 2020) throw new ArgumentOutOfRangeException(nameof(year), "Năm không hợp lệ.");
        if (targetRevenue < 0) throw new ArgumentException("Chỉ tiêu doanh thu không được âm.", nameof(targetRevenue));
        if (targetOrders < 0) throw new ArgumentException("Chỉ tiêu đơn hàng không được âm.", nameof(targetOrders));

        return new KpiTarget
        {
            Id = Guid.NewGuid(),
            StaffId = staffId,
            StaffName = staffName.Trim(),
            StoreId = storeId,
            StoreName = storeName?.Trim(),
            Month = month,
            Year = year,
            TargetRevenue = targetRevenue,
            TargetOrders = targetOrders,
            Notes = notes?.Trim()
        };
    }

    public void Update(decimal targetRevenue, int targetOrders, string? notes)
    {
        if (targetRevenue < 0) throw new ArgumentException("Chỉ tiêu doanh thu không được âm.", nameof(targetRevenue));
        if (targetOrders < 0) throw new ArgumentException("Chỉ tiêu đơn hàng không được âm.", nameof(targetOrders));

        TargetRevenue = targetRevenue;
        TargetOrders = targetOrders;
        Notes = notes?.Trim();
    }
}
