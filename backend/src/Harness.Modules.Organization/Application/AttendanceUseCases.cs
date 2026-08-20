using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Organization.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Organization.Application;

public record AttendanceDto(
    Guid Id,
    Guid StaffId,
    string StaffName,
    Guid StoreId,
    string StoreName,
    DateOnly WorkDate,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    AttendanceStatus Status,
    string StatusText,
    string? Notes);

public record GetAttendanceQuery(Guid? StaffId, Guid? StoreId, DateOnly? FromDate, DateOnly? ToDate) : IRequest<List<AttendanceDto>>;

public class GetAttendanceQueryHandler : IRequestHandler<GetAttendanceQuery, List<AttendanceDto>>
{
    private readonly IHarnessDbContext _db;
    public GetAttendanceQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<List<AttendanceDto>> Handle(GetAttendanceQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<AttendanceRecord>().AsNoTracking();

        if (request.StaffId.HasValue) query = query.Where(a => a.StaffId == request.StaffId.Value);
        if (request.StoreId.HasValue) query = query.Where(a => a.StoreId == request.StoreId.Value);
        if (request.FromDate.HasValue) query = query.Where(a => a.WorkDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(a => a.WorkDate <= request.ToDate.Value);

        var list = await query.OrderByDescending(a => a.WorkDate).ThenBy(a => a.StaffName).ToListAsync(cancellationToken);

        return list.Select(a => new AttendanceDto(
            a.Id,
            a.StaffId,
            a.StaffName,
            a.StoreId,
            a.StoreName,
            a.WorkDate,
            a.CheckInTime,
            a.CheckOutTime,
            a.Status,
            a.Status switch
            {
                AttendanceStatus.Present => "Đúng giờ",
                AttendanceStatus.Late => "Đi muộn",
                AttendanceStatus.Absent => "Vắng mặt",
                AttendanceStatus.EarlyLeave => "Về sớm",
                _ => "Không xác định"
            },
            a.Notes
        )).ToList();
    }
}

public record CheckInCommand(Guid StaffId, string StaffName, Guid StoreId, string StoreName, string? Notes) : IRequest<Guid>;

public class CheckInCommandHandler : IRequestHandler<CheckInCommand, Guid>
{
    private readonly IHarnessDbContext _db;
    public CheckInCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<Guid> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.LocalDateTime);

        var existing = await _db.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(a => a.StaffId == request.StaffId && a.WorkDate == today, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"Nhân viên {request.StaffName} đã điểm danh check-in ngày hôm nay.");
        }

        var record = AttendanceRecord.CreateCheckIn(request.StaffId, request.StaffName, request.StoreId, request.StoreName, now, request.Notes);
        _db.Set<AttendanceRecord>().Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        return record.Id;
    }
}

public record CheckOutCommand(Guid AttendanceId, string? Notes) : IRequest<bool>;

public class CheckOutCommandHandler : IRequestHandler<CheckOutCommand, bool>
{
    private readonly IHarnessDbContext _db;
    public CheckOutCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<bool> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var record = await _db.Set<AttendanceRecord>().FirstOrDefaultAsync(a => a.Id == request.AttendanceId, cancellationToken);
        if (record == null) return false;

        record.RecordCheckOut(DateTimeOffset.UtcNow, request.Notes);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record SaveAttendanceRecordCommand(
    Guid? ExistingId,
    Guid StaffId,
    string StaffName,
    Guid StoreId,
    string StoreName,
    DateOnly WorkDate,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    AttendanceStatus Status,
    string? Notes) : IRequest<Guid>;

public class SaveAttendanceRecordCommandHandler : IRequestHandler<SaveAttendanceRecordCommand, Guid>
{
    private readonly IHarnessDbContext _db;
    public SaveAttendanceRecordCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<Guid> Handle(SaveAttendanceRecordCommand request, CancellationToken cancellationToken)
    {
        if (request.ExistingId.HasValue)
        {
            var existing = await _db.Set<AttendanceRecord>().FirstOrDefaultAsync(a => a.Id == request.ExistingId.Value, cancellationToken);
            if (existing != null)
            {
                var updated = AttendanceRecord.ManualCreateOrUpdate(
                    existing.Id, request.StaffId, request.StaffName, request.StoreId, request.StoreName,
                    request.WorkDate, request.CheckInTime, request.CheckOutTime, request.Status, request.Notes);

                _db.Set<AttendanceRecord>().Remove(existing);
                _db.Set<AttendanceRecord>().Add(updated);
                await _db.SaveChangesAsync(cancellationToken);
                return updated.Id;
            }
        }

        var newRecord = AttendanceRecord.ManualCreateOrUpdate(
            null, request.StaffId, request.StaffName, request.StoreId, request.StoreName,
            request.WorkDate, request.CheckInTime, request.CheckOutTime, request.Status, request.Notes);

        _db.Set<AttendanceRecord>().Add(newRecord);
        await _db.SaveChangesAsync(cancellationToken);
        return newRecord.Id;
    }
}
