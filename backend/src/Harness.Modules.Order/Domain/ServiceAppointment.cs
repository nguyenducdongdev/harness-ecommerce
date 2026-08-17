using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Order.Domain;

public enum ServiceAppointmentType { Installation = 1, Measurement = 2 }
public enum ServiceAppointmentStatus { Requested = 1, Confirmed = 2, Completed = 3, Cancelled = 4 }

/// <summary>
/// Lịch hẹn dịch vụ tại nhà: lắp đặt (giao + lắp tận nơi) hoặc đo đạc riêng (tủ bếp, tủ áo bespoke).
/// Đặc thù ngành nội thất — gắn với đơn hàng (OrderId) nếu có, hoặc đặt lịch đo đạc độc lập.
/// </summary>
public class ServiceAppointment : AuditableEntity<Guid>
{
    public string CustomerPhone { get; private set; } = default!;
    public string CustomerName { get; private set; } = default!;
    public string ReceiverName { get; private set; } = default!;
    public string ReceiverPhone { get; private set; } = default!;
    public string Address { get; private set; } = default!;

    public ServiceAppointmentType AppointmentType { get; private set; }
    public DateOnly DesiredDate { get; private set; }
    public string TimeSlot { get; private set; } = default!; // "buoi-sang" | "buoi-chieu" | giờ cụ thể
    public string? Note { get; private set; }
    public Guid? OrderId { get; private set; }
    public ServiceAppointmentStatus Status { get; private set; } = ServiceAppointmentStatus.Requested;

    private ServiceAppointment() { } // EF

    public static ServiceAppointment Create(
        string customerPhone, string customerName, string receiverName, string receiverPhone,
        string address, ServiceAppointmentType type, DateOnly desiredDate, string timeSlot,
        string? note = null, Guid? orderId = null)
    {
        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new ArgumentException("Số điện thoại khách hàng là bắt buộc.", nameof(customerPhone));
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (desiredDate < today)
            throw new ArgumentException("Ngày hẹn phải từ hôm nay trở đi.", nameof(desiredDate));

        return new ServiceAppointment
        {
            Id = Guid.NewGuid(),
            CustomerPhone = customerPhone,
            CustomerName = customerName.Trim(),
            ReceiverName = receiverName.Trim(),
            ReceiverPhone = receiverPhone,
            Address = address.Trim(),
            AppointmentType = type,
            DesiredDate = desiredDate,
            TimeSlot = timeSlot,
            Note = note,
            OrderId = orderId
        };
    }

    public void Confirm()
    {
        EnsureNotFinished();
        Status = ServiceAppointmentStatus.Confirmed;
    }

    public void Complete()
    {
        if (Status != ServiceAppointmentStatus.Confirmed)
            throw new InvalidOperationException("Chỉ có thể hoàn thành lịch đã xác nhận.");
        Status = ServiceAppointmentStatus.Completed;
    }

    public void Cancel()
    {
        EnsureNotFinished();
        Status = ServiceAppointmentStatus.Cancelled;
    }

    private void EnsureNotFinished()
    {
        if (Status is ServiceAppointmentStatus.Completed or ServiceAppointmentStatus.Cancelled)
            throw new InvalidOperationException($"Không thể thay đổi lịch ở trạng thái {Status}.");
    }
}
