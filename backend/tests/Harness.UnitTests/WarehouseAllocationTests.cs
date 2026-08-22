using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Inventory.Application;
using Harness.Modules.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Harness.UnitTests;

/// <summary>
/// M15 — Multi-Showroom Inventory Auto-Allocation:
/// kiểm thử công thức Haversine và bộ phân bổ kho gần nhất có đủ tồn.
/// </summary>
public class WarehouseAllocationTests
{
    [Fact]
    public void Haversine_SamePoint_IsZeroKm()
    {
        var km = Haversine.Kilometers(10.7723, 106.7043, 10.7723, 106.7043);
        Assert.Equal(0, km, 3);
    }

    [Fact]
    public void Haversine_HanoiToHcm_IsAbout1140Km()
    {
        var km = Haversine.Kilometers(21.0285, 105.7892, 10.7723, 106.7043);
        // Hà Nội ↔ TP.HCM ≈ 1.144 km đường chim bay theo Haversine (bán kính 6371km).
        Assert.InRange(km, 1100, 1200);
    }

    [Fact]
    public void Haversine_Symmetric()
    {
        var a = Haversine.Kilometers(10.77, 106.70, 21.03, 105.79);
        var b = Haversine.Kilometers(21.03, 105.79, 10.77, 106.70);
        Assert.Equal(a, b, 6);
    }

    [Fact]
    public async Task Nearest_WithoutSku_PicksClosestActiveWarehouse()
    {
        // Giao tại TP.HCM → Showroom Q1 (gần nhất) được chọn.
        var warehouses = new[]
        {
            Wh(1, "HCM-01", 10.7723, 106.7043, true),
            Wh(2, "HN-01", 21.0285, 105.7892, true),
            Wh(3, "KHO-TONG", 10.9556, 106.6890, false)
        };

        using var db = new TestInventoryDb(warehouses);
        var allocator = new NearestWarehouseAllocator(db);

        var result = await allocator.FindNearestAsync(10.80, 106.70);

        Assert.Equal(1, result.WarehouseId);
        Assert.Contains("HCM-01", result.Reason);
    }

    [Fact]
    public async Task Nearest_PrefersWarehouseWithEnoughStock()
    {
        // Giao gần Hà Nội nhưng SKU-A chỉ có ở HCM-01 → chọn HCM-01 (kho duy nhất đủ tồn).
        var warehouses = new[]
        {
            Wh(1, "HCM-01", 10.7723, 106.7043, true),
            Wh(2, "HN-01", 21.0285, 105.7892, true)
        };

        using var db = new TestInventoryDb(warehouses);
        db.AddStock(1, "SKU-A", 5);
        var allocator = new NearestWarehouseAllocator(db);

        var result = await allocator.FindNearestAsync(21.00, 105.80, new Dictionary<string, int> { ["SKU-A"] = 3 });

        Assert.Equal(1, result.WarehouseId);
    }

    [Fact]
    public async Task Nearest_WithoutAnyStock_FallsBackToClosest()
    {
        // Không kho nào có đủ SKU-A → fallback về kho gần nhất (HCM-01).
        var warehouses = new[]
        {
            Wh(1, "HCM-01", 10.7723, 106.7043, true),
            Wh(2, "HN-01", 21.0285, 105.7892, true)
        };

        using var db = new TestInventoryDb(warehouses);
        db.AddStock(1, "SKU-A", 1);
        var allocator = new NearestWarehouseAllocator(db);

        var result = await allocator.FindNearestAsync(10.80, 106.70, new Dictionary<string, int> { ["SKU-A"] = 10 });

        Assert.Equal(1, result.WarehouseId);
        Assert.Contains("Không có kho đủ tồn", result.Reason);
    }

    [Fact]
    public async Task Nearest_NoWarehouseWithCoordinates_ReturnsNull()
    {
        using var db = new TestInventoryDb(new[] { Wh(1, "HCM-01", null, null, true) });
        var allocator = new NearestWarehouseAllocator(db);

        var result = await allocator.FindNearestAsync(10.80, 106.70);

        Assert.Null(result.WarehouseId);
    }

    private static (int Id, string Code, double? Lat, double? Lng, bool Showroom) Wh(
        int id, string code, double? lat, double? lng, bool showroom)
        => (id, code, lat, lng, showroom);
}

/// <summary>Fake DbContext cho unit test — chỉ dùng InMemory, không chạm PostgreSQL thật.</summary>
internal sealed class TestInventoryDb : DbContext, IHarnessDbContext
{
    private readonly (int Id, string Code, double? Lat, double? Lng, bool Showroom)[] _warehouses;

    public TestInventoryDb((int Id, string Code, double? Lat, double? Lng, bool Showroom)[] warehouses)
    {
        _warehouses = warehouses;
        foreach (var w in _warehouses)
        {
            var entity = new Warehouse
            {
                Code = w.Code,
                Name = w.Code,
                Address = w.Code,
                IsShowroom = w.Showroom,
                IsActive = true,
                Latitude = w.Lat,
                Longitude = w.Lng
            };
            Add(entity);
            Entry(entity).Property(e => e.Id).CurrentValue = w.Id;
        }
        SaveChanges();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"inventory-test-{Guid.NewGuid():N}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Warehouse>().HasKey(x => x.Id);
        modelBuilder.Entity<StockLevel>().HasKey(x => x.Id);
    }

    public void AddStock(int warehouseId, string sku, int qty)
    {
        Set<StockLevel>().Add(StockLevel.Create(warehouseId, sku, qty));
        SaveChanges();
    }
}