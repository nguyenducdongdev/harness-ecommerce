namespace Harness.BuildingBlocks.Application.Abstractions;

/// <summary>Kết quả phân bổ kho cho đơn hàng.</summary>
public record AllocationResult(int? WarehouseId, double DistanceKm, string Reason);

/// <summary>
/// M15 — Multi-Showroom Inventory Auto-Allocation:
/// tự động chọn showroom/kho gần nhất với địa chỉ giao hàng (Haversine),
/// ưu tiên kho có đủ tồn kho (available ≥ tổng nhu cầu các SKU trong đơn).
/// </summary>
public interface IWarehouseAllocator
{
    /// <summary>
    /// Chọn kho xử lý đơn gần nhất theo toạ độ giao hàng.
    /// Khi <paramref name="requiredSkus"/> có nhu cầu, chỉ xét các kho có đủ tồn kho;
    /// nếu không kho nào đủ, fallback về kho gần nhất (kể cả thiếu tồn).
    /// Kho không có toạ độ (Lat/Lng null) bị loại khỏi xét duyệt.
    /// </summary>
    Task<AllocationResult> FindNearestAsync(
        double latitude, double longitude, IReadOnlyDictionary<string, int>? requiredSkus = null,
        CancellationToken cancellationToken = default);
}