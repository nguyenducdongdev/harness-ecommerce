using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Shipping.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Shipping.Application;

public record CreateShipmentCommand(Guid OrderId, Carrier Carrier, decimal ShippingFee) : IRequest<ShipmentDto>;

public class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ShippingFee).GreaterThanOrEqualTo(0);
    }
}

public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, ShipmentDto>
{
    private readonly IHarnessDbContext _db;

    public CreateShipmentCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ShipmentDto> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = Shipment.Create(request.OrderId, request.Carrier, request.ShippingFee);
        _db.Set<Shipment>().Add(shipment);
        await _db.SaveChangesAsync(cancellationToken);
        return ShipmentMapper.ToDto(shipment);
    }
}

public record UpdateShipmentStatusCommand(Guid ShipmentId, ShipmentStatus Status) : IRequest<ShipmentDto>;

public class UpdateShipmentStatusCommandHandler : IRequestHandler<UpdateShipmentStatusCommand, ShipmentDto>
{
    private readonly IHarnessDbContext _db;

    public UpdateShipmentStatusCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ShipmentDto> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _db.Set<Shipment>().FindAsync(new object[] { request.ShipmentId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy lô hàng #{request.ShipmentId}.");

        shipment.UpdateStatus(request.Status);
        _db.AddToOutbox(new ShipmentStatusChangedIntegrationEvent(
            shipment.OrderId, shipment.TrackingCode, request.Status.ToString()));
        await _db.SaveChangesAsync(cancellationToken);
        return ShipmentMapper.ToDto(shipment);
    }
}

public record GetShipmentByOrderQuery(Guid OrderId) : IRequest<ShipmentDto?>;

public class GetShipmentByOrderQueryHandler : IRequestHandler<GetShipmentByOrderQuery, ShipmentDto?>
{
    private readonly IHarnessDbContext _db;

    public GetShipmentByOrderQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<ShipmentDto?> Handle(GetShipmentByOrderQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _db.Set<Shipment>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
        return shipment is null ? null : ShipmentMapper.ToDto(shipment);
    }
}

public record ShipmentDto(Guid Id, Guid OrderId, string Carrier, string TrackingCode, string Status,
    decimal ShippingFee, DateTimeOffset? EstimatedDeliveryAt, DateTimeOffset? DeliveredAt);

internal static class ShipmentMapper
{
    public static ShipmentDto ToDto(Shipment s) => new(
        s.Id, s.OrderId, s.Carrier.ToString(), s.TrackingCode, s.Status.ToString(),
        s.ShippingFee, s.EstimatedDeliveryAt, s.DeliveredAt);
}
