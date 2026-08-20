namespace Harness.Modules.Organization.Domain;

public enum AttendanceStatus
{
    Present = 1,    // Đúng giờ
    Late = 2,       // Đi muộn
    Absent = 3,     // Vắng mặt
    EarlyLeave = 4  // Về sớm
}
