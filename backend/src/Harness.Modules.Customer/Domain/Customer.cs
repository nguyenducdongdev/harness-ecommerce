using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Customer.Domain;

public class Customer : AuditableEntity<Guid>
{
    public string FullName { get; private set; } = default!;
    /// <summary>Số điện thoại = định danh chính của khách VN (đăng nhập OTP Phase 2).</summary>
    public string Phone { get; private set; } = default!;
    public string? Email { get; private set; }
    public DateTimeOffset? DateOfBirth { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<CustomerAddress> _addresses = new();
    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

    private Customer() { } // EF

    public static Customer Register(string fullName, string phone, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Số điện thoại là bắt buộc.");
        return new Customer { Id = Guid.NewGuid(), FullName = fullName, Phone = phone, Email = email };
    }

    public void AddAddress(string label, string receiverName, string phone, string fullAddress, bool isDefault = false)
    {
        if (isDefault) foreach (var a in _addresses) a.SetDefault(false);
        _addresses.Add(new CustomerAddress
        {
            Id = Guid.NewGuid(), CustomerId = Id, Label = label,
            ReceiverName = receiverName, Phone = phone, FullAddress = fullAddress, IsDefault = isDefault
        });
    }
}

public class CustomerAddress : Entity<Guid>
{
    public Guid CustomerId { get; set; }
    public string Label { get; set; } = "Nhà";
    public string ReceiverName { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string FullAddress { get; set; } = default!;
    public bool IsDefault { get; private set; }

    public void SetDefault(bool value) => IsDefault = value;
}
