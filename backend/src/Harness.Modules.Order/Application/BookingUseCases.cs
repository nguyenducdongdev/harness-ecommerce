using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Order.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Order.Application;

public record CreateBookingCommand(
    string CustomerPhone,
    string CustomerName,
    string ReceiverName,
    string ReceiverPhone,
    string Address,
    ServiceAppointmentType AppointmentType,
    DateOnly DesiredDate,
    string TimeSlot,
    string? Note = null,
    Guid? OrderId = null) : IRequest<BookingDto>;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.CustomerPhone).NotEmpty().Matches(@"^0\d{9,10}$").WithMessage("SĐT khách hàng không hợp lệ.");
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReceiverPhone).NotEmpty().Matches(@"^0\d{9,10}$");
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AppointmentType).IsInEnum();
        RuleFor(x => x.TimeSlot).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DesiredDate).NotEmpty();
        RuleFor(x => x.DesiredDate).Must(d => d >= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Ngày hẹn phải từ hôm nay trở đi.");
    }
}

public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
{
    private readonly IHarnessDbContext _db;

    public CreateBookingCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var appointment = ServiceAppointment.Create(
            request.CustomerPhone, request.CustomerName, request.ReceiverName, request.ReceiverPhone,
            request.Address, request.AppointmentType, request.DesiredDate, request.TimeSlot,
            request.Note, request.OrderId);

        _db.Set<ServiceAppointment>().Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);
        return BookingMapper.ToDto(appointment);
    }
}

public record GetBookingsByPhoneQuery(string Phone) : IRequest<IReadOnlyList<BookingDto>>;

public class GetBookingsByPhoneQueryHandler : IRequestHandler<GetBookingsByPhoneQuery, IReadOnlyList<BookingDto>>
{
    private readonly IHarnessDbContext _db;

    public GetBookingsByPhoneQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<BookingDto>> Handle(GetBookingsByPhoneQuery request, CancellationToken cancellationToken)
        => await _db.Set<ServiceAppointment>().AsNoTracking()
            .Where(b => b.CustomerPhone == request.Phone)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => BookingMapper.ToDto(b))
            .ToListAsync(cancellationToken);
}

public record UpdateBookingStatusCommand(Guid Id, ServiceAppointmentStatus NewStatus) : IRequest<BookingDto>;

public class UpdateBookingStatusCommandValidator : AbstractValidator<UpdateBookingStatusCommand>
{
    public UpdateBookingStatusCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, BookingDto>
{
    private readonly IHarnessDbContext _db;

    public UpdateBookingStatusCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<BookingDto> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _db.Set<ServiceAppointment>()
            .FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy lịch hẹn #{request.Id}.");

        switch (request.NewStatus)
        {
            case ServiceAppointmentStatus.Confirmed: appointment.Confirm(); break;
            case ServiceAppointmentStatus.Completed: appointment.Complete(); break;
            case ServiceAppointmentStatus.Cancelled: appointment.Cancel(); break;
            default:
                throw new InvalidOperationException($"Trạng thái {request.NewStatus} không hỗ trợ thay đổi.");
        }

        await _db.SaveChangesAsync(cancellationToken);
        return BookingMapper.ToDto(appointment);
    }
}

public record BookingDto(
    Guid Id, string CustomerPhone, string CustomerName, string ReceiverName, string ReceiverPhone,
    string Address, string AppointmentType, DateOnly DesiredDate, string TimeSlot,
    string? Note, Guid? OrderId, string Status);

internal static class BookingMapper
{
    public static BookingDto ToDto(ServiceAppointment b) => new(
        b.Id, b.CustomerPhone, b.CustomerName, b.ReceiverName, b.ReceiverPhone,
        b.Address, b.AppointmentType.ToString(), b.DesiredDate, b.TimeSlot,
        b.Note, b.OrderId, b.Status.ToString());
}
