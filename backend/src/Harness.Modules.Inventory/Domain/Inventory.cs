using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Inventory.Domain;

/// <summary>Kho/showroom — mỗi cửa hàng chuỗi là 1 warehouse (IsShowroom = true).</summary>
public class Warehouse : AuditableEntity<int>
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string? Phone { get; set; }
    public bool IsShowroom { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Tồn kho theo (kho × SKU biến thể).</summary>
public class StockLevel : Entity<Guid>
{
    public int WarehouseId { get; private set; }
    public string VariantSku { get; private set; } = default!;
    public int QuantityAvailable { get; private set; }
    public int QuantityReserved { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private StockLevel() { }

    public static StockLevel Create(int warehouseId, string variantSku, int initialQuantity = 0)
    {
        if (initialQuantity < 0) throw new ArgumentException("Tồn kho không được âm.");
        return new StockLevel
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            VariantSku = variantSku,
            QuantityAvailable = initialQuantity
        };
    }

    public void Adjust(int delta)
    {
        if (QuantityAvailable + delta < 0)
            throw new InvalidOperationException($"Không đủ tồn kho cho SKU {VariantSku} (hiện có {QuantityAvailable}).");
        QuantityAvailable += delta;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Giữ chỗ (reserve) khi có đơn: chuyển khả dụng → đã giữ chỗ; không đủ thì báo lỗi.</summary>
    public void Reserve(int quantity)
    {
        EnsurePositive(quantity);
        if (QuantityAvailable < quantity)
            throw new InvalidOperationException(
                $"Không đủ tồn kho tại showroom cho SKU {VariantSku} (khả dụng {QuantityAvailable}, cần {quantity}).");
        QuantityAvailable -= quantity;
        QuantityReserved += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Hoàn lại lượng giữ chỗ khi hủy đơn: đã giữ chỗ → khả dụng.</summary>
    public void ReleaseReservation(int quantity)
    {
        EnsurePositive(quantity);
        if (QuantityReserved < quantity)
            throw new InvalidOperationException(
                $"Chỉ đang giữ chỗ {QuantityReserved} cho SKU {VariantSku}, không thể hoàn {quantity}.");
        QuantityReserved -= quantity;
        QuantityAvailable += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void EnsurePositive(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng phải dương.", nameof(quantity));
    }
}

public enum MovementType { Inbound = 1, Outbound = 2, TransferOut = 3, TransferIn = 4, Adjustment = 5, Reservation = 6, Release = 7 }

/// <summary>Lịch sử xuất nhập — dùng để truy vết và đối soát với DMS/ERP.</summary>
public class StockMovement : Entity<Guid>
{
    public int WarehouseId { get; set; }
    public string VariantSku { get; set; } = default!;
    public MovementType Type { get; set; }
    public int Quantity { get; set; } // dương = nhập, âm không dùng — hướng nằm ở Type
    public string Reference { get; set; } = default!; // mã đơn/phiếu chuyển kho
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record StockChangedIntegrationEvent(int WarehouseId, string VariantSku, int NewQuantity, string Reason) : IntegrationEvent
{
    public override string EventType => "inventory.stock-changed";
}
