using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Shipping.Domain;

public enum Carrier { Ghn = 1, Ghtk = 2, ViettelPost = 3, SelfDelivery = 4 }
public enum ShipmentStatus { Created = 1, PickedUp = 2, InTransit = 3, Delivered = 4, Returned = 5, Lost = 6 }

/// <summary>Lô hàng của đơn — kết nối hãng vận chuyển (GHN/GHTK/VTP).</summary>
public class Shipment : AuditableEntity<Guid>
{
    public Guid OrderId { get; private set; }
    public Carrier Carrier { get; private set; }
    public string TrackingCode { get; private set; } = default!;
    public ShipmentStatus Status { get; private set; } = ShipmentStatus.Created;
    public decimal ShippingFee { get; private set; }
    public DateTimeOffset? EstimatedDeliveryAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }

    private Shipment() { } // EF

    public static Shipment Create(Guid orderId, Carrier carrier, decimal shippingFee, DateTimeOffset? estimatedDeliveryAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Carrier = carrier,
            TrackingCode = $"HV{DateTime.UtcNow:yyMMddHHmmss}",
            ShippingFee = shippingFee,
            EstimatedDeliveryAt = estimatedDeliveryAt
        };

    public void UpdateStatus(ShipmentStatus status)
    {
        Status = status;
        if (status == ShipmentStatus.Delivered) DeliveredAt = DateTimeOffset.UtcNow;
    }
}

public sealed record ShipmentStatusChangedIntegrationEvent(Guid OrderId, string TrackingCode, string NewStatus) : IntegrationEvent
{
    public override string EventType => "shipping.status-changed";
}
