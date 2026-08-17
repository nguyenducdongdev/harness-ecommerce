using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Order.Domain;

public enum OrderStatus
{
    PendingConfirmation = 1, // Chờ xác nhận
    Processing = 2,          // Đang xử lý
    Shipping = 3,            // Đang giao
    Delivered = 4,           // Đã giao
    Completed = 5,           // Hoàn thành
    Cancelled = 6,           // Đã hủy
    Refunded = 7             // Đã hoàn tiền
}

public enum DeliveryMethod { Standard = 1, Express = 2, PickupAtStore = 3 }
public enum PaymentMethod { Cod = 1, BankTransfer = 2, VnPay = 3, MoMo = 4, ZaloPay = 5 }

/// <summary>Đơn hàng — aggregate root. Trạng thái chuyển theo máy trạng thái ValidateStatusTransition.</summary>
public class Order : AuditableEntity<Guid>
{
    public string OrderNumber { get; private set; } = default!;

    public string CustomerName { get; private set; } = default!;
    public string CustomerPhone { get; private set; } = default!;
    public string? CustomerEmail { get; private set; }
    public string ShippingAddress { get; private set; } = default!;
    public string? Note { get; private set; }

    public DeliveryMethod DeliveryMethod { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.PendingConfirmation;

    public decimal ItemsTotal { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount => ItemsTotal + ShippingFee - DiscountAmount;
    public int? WarehouseId { get; private set; } // kho/showroom xử lý đơn

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF

    public static Order Create(
        string customerName, string customerPhone, string? customerEmail,
        string shippingAddress, string? note,
        DeliveryMethod deliveryMethod, PaymentMethod paymentMethod,
        IEnumerable<(int productId, string variantSku, string productName, decimal unitPrice, int quantity)> items,
        decimal shippingFee = 0, decimal discountAmount = 0, int? warehouseId = null)
    {
        if (string.IsNullOrWhiteSpace(customerPhone))
            throw new ArgumentException("Số điện thoại khách hàng là bắt buộc.");
        var itemList = items.ToList();
        if (itemList.Count == 0) throw new ArgumentException("Đơn hàng phải có ít nhất 1 sản phẩm.");
        if (itemList.Any(i => i.quantity <= 0)) throw new ArgumentException("Số lượng phải lớn hơn 0.");
        if (itemList.Any(i => i.unitPrice < 0)) throw new ArgumentException("Đơn giá không được âm.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"HD{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            CustomerEmail = customerEmail,
            ShippingAddress = shippingAddress,
            Note = note,
            DeliveryMethod = deliveryMethod,
            PaymentMethod = paymentMethod,
            ItemsTotal = itemList.Sum(i => i.unitPrice * i.quantity),
            ShippingFee = shippingFee,
            DiscountAmount = discountAmount,
            WarehouseId = warehouseId
        };

        foreach (var (productId, sku, name, price, qty) in itemList)
            order._items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = productId,
                VariantSku = sku,
                ProductName = name,
                UnitPrice = price,
                Quantity = qty
            });

        return order;
    }

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.PendingConfirmation] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
        [OrderStatus.Shipping] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = new[] { OrderStatus.Completed, OrderStatus.Refunded },
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = new[] { OrderStatus.Refunded },
        [OrderStatus.Refunded] = Array.Empty<OrderStatus>()
    };

    public void TransitionTo(OrderStatus next)
    {
        if (!AllowedTransitions[Status].Contains(next))
            throw new InvalidOperationException($"Không thể chuyển trạng thái từ {Status} sang {next}.");
        Status = next;
    }
}

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; set; }
    public int ProductId { get; set; }
    public string VariantSku { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

// ===== Integration events (ERP/DMS/sản xuất consume) =====

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, string OrderNumber, decimal TotalAmount,
    string CustomerPhone, string DeliveryMethod, string PaymentMethod) : IntegrationEvent
{
    public override string EventType => "order.created";
}

public sealed record OrderStatusChangedIntegrationEvent(Guid OrderId, string OrderNumber, string NewStatus) : IntegrationEvent
{
    public override string EventType => "order.status-changed";
}
