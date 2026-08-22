using Harness.BuildingBlocks.Application.Abstractions;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Inventory.Application;

public class NearestWarehouseAllocator : IWarehouseAllocator
{
    private readonly IHarnessDbContext _db;

    public NearestWarehouseAllocator(IHarnessDbContext db) => _db = db;

    public async Task<AllocationResult> FindNearestAsync(
        double latitude, double longitude, IReadOnlyDictionary<string, int>? requiredSkus = null,
        CancellationToken cancellationToken = default)
    {
        var warehouses = await _db.Set<Warehouse>().AsNoTracking()
            .Where(w => w.IsActive && w.Latitude.HasValue && w.Longitude.HasValue)
            .ToListAsync(cancellationToken);

        if (warehouses.Count == 0)
            return new AllocationResult(null, double.MaxValue, "Không có kho active có toạ độ.");

        // Nhóm theo "có đủ tồn cho mọi SKU" hay không.
        var candidates = new List<(Warehouse w, double dist, bool hasStock)>();

        foreach (var w in warehouses)
        {
            var dist = Haversine.Kilometers(latitude, longitude, w.Latitude!.Value, w.Longitude!.Value);
            bool hasStock = true;
            if (requiredSkus is { Count: > 0 })
            {
                foreach (var (sku, qty) in requiredSkus)
                {
                    var level = await _db.Set<StockLevel>().AsNoTracking()
                        .FirstOrDefaultAsync(s => s.WarehouseId == w.Id && s.VariantSku == sku, cancellationToken);
                    if (level is null || level.QuantityAvailable < qty)
                    {
                        hasStock = false;
                        break;
                    }
                }
            }
            candidates.Add((w, dist, hasStock));
        }

        // Ưu tiên kho có đủ stock trước; nếu không có, lấy kho gần nhất bất kỳ.
        var chosen = candidates.Where(c => c.hasStock)
            .OrderBy(c => c.dist)
            .FirstOrDefault();

        if (chosen.w is null)
        {
            chosen = candidates.OrderBy(c => c.dist).First();
            return new AllocationResult(chosen.w.Id, Math.Round(chosen.dist, 1),
                $"Không có kho đủ tồn cho {string.Join(", ", requiredSkus?.Keys ?? Array.Empty<string>())}; chọn kho gần nhất {chosen.w.Name}.");
        }

        return new AllocationResult(chosen.w.Id, Math.Round(chosen.dist, 1),
            $"Tự động chọn {chosen.w.Name} — kho gần nhất có đủ tồn.");
    }
}

/// <summary>Công thức Haversine tính khoảng cách (km) giữa hai toạ độ địa lý.</summary>
public static class Haversine
{
    private const double EarthRadiusKm = 6371.0;

    public static double Kilometers(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
