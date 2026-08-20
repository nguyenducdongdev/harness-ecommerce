using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Organization.Domain;

/// <summary>
/// Bản ghi Chấm công nhân viên tại cửa hàng.
/// </summary>
public class AttendanceRecord : AuditableEntity<Guid>
{
    public Guid StaffId { get; private set; }
    public string StaffName { get; private set; } = default!;
    public Guid StoreId { get; private set; }
    public string StoreName { get; private set; } = default!;
    public DateOnly WorkDate { get; private set; }
    public DateTimeOffset? CheckInTime { get; private set; }
    public DateTimeOffset? CheckOutTime { get; private set; }
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.Present;
    public string? Notes { get; private set; }

    private AttendanceRecord() { }

    public static AttendanceRecord CreateCheckIn(Guid staffId, string staffName, Guid storeId, string storeName, DateTimeOffset checkInTime, string? notes = null)
    {
        var localTime = checkInTime.ToLocalTime();
        var status = localTime.Hour > 8 || (localTime.Hour == 8 && localTime.Minute > 30)
            ? AttendanceStatus.Late
            : AttendanceStatus.Present;

        return new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            StaffId = staffId,
            StaffName = staffName.Trim(),
            StoreId = storeId,
            StoreName = storeName.Trim(),
            WorkDate = DateOnly.FromDateTime(checkInTime.LocalDateTime),
            CheckInTime = checkInTime,
            Status = status,
            Notes = notes?.Trim()
        };
    }

    public void RecordCheckOut(DateTimeOffset checkOutTime, string? notes = null)
    {
        CheckOutTime = checkOutTime;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes} | {notes.Trim()}";
        }
    }

    public static AttendanceRecord ManualCreateOrUpdate(
        Guid? existingId, Guid staffId, string staffName, Guid storeId, string storeName,
        DateOnly workDate, DateTimeOffset? checkInTime, DateTimeOffset? checkOutTime,
        AttendanceStatus status, string? notes)
    {
        return new AttendanceRecord
        {
            Id = existingId ?? Guid.NewGuid(),
            StaffId = staffId,
            StaffName = staffName.Trim(),
            StoreId = storeId,
            StoreName = storeName.Trim(),
            WorkDate = workDate,
            CheckInTime = checkInTime,
            CheckOutTime = checkOutTime,
            Status = status,
            Notes = notes?.Trim()
        };
    }
}
