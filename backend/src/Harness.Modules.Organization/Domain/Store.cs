using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Organization.Domain;

/// <summary>
/// Quản lý Cửa hàng / Showroom trong hệ thống.
/// </summary>
public class Store : AuditableEntity<Guid>
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? ManagerName { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Store() { }

    public static Store Create(string code, string name, string address, string phone, string? managerName = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Mã cửa hàng không được trống.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên cửa hàng không được trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Địa chỉ cửa hàng không được trống.", nameof(address));

        return new Store
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpper(),
            Name = name.Trim(),
            Address = address.Trim(),
            Phone = phone?.Trim() ?? string.Empty,
            ManagerName = managerName?.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string address, string phone, string? managerName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tên cửa hàng không được trống.", nameof(name));
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Địa chỉ cửa hàng không được trống.", nameof(address));

        Name = name.Trim();
        Address = address.Trim();
        Phone = phone?.Trim() ?? string.Empty;
        ManagerName = managerName?.Trim();
        IsActive = isActive;
    }
}
