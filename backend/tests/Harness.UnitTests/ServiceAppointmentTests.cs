using Harness.Modules.Order.Domain;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử lịch hẹn dịch vụ tại nhà (lắp đặt / đo đạc).</summary>
public class ServiceAppointmentTests
{
    private static ServiceAppointment NewBooking(int daysAhead = 2, ServiceAppointmentType type = ServiceAppointmentType.Installation)
        => ServiceAppointment.Create(
            "0912345678", "Nguyễn Văn A", "Nguyễn Văn A", "0912345678",
            "123 Lê Lợi, Quận 1", type,
            DateOnly.FromDateTime(DateTime.Today.AddDays(daysAhead)), "buoi-sang");

    [Fact]
    public void Create_ValidRequest_DefaultsToRequested()
    {
        var booking = NewBooking();
        Assert.Equal(ServiceAppointmentStatus.Requested, booking.Status);
        Assert.Equal(ServiceAppointmentType.Installation, booking.AppointmentType);
    }

    [Fact]
    public void Create_PastDesiredDate_Throws()
        => Assert.Throws<ArgumentException>(() => NewBooking(daysAhead: -1));

    [Fact]
    public void Confirm_ThenComplete_Transitions()
    {
        var booking = NewBooking(type: ServiceAppointmentType.Measurement);
        booking.Confirm();
        Assert.Equal(ServiceAppointmentStatus.Confirmed, booking.Status);

        booking.Complete();
        Assert.Equal(ServiceAppointmentStatus.Completed, booking.Status);
    }

    [Fact]
    public void Complete_WithoutConfirm_Throws()
        => Assert.Throws<InvalidOperationException>(() => NewBooking().Complete());

    [Fact]
    public void Cancel_AfterCompleted_Throws()
    {
        var booking = NewBooking();
        booking.Confirm();
        booking.Complete();

        Assert.Throws<InvalidOperationException>(() => booking.Cancel());
    }
}
