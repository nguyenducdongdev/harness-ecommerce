using Harness.Api.Persistence;
using Harness.Modules.Catalog.Domain;
using Harness.Modules.Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Harness.IntegrationTests;

/// <summary>
/// Integration test dùng EF InMemory — kiểm tra model mapping + seed + truy vấn cơ bản.
/// (Test end-to-end với PostgreSQL thật sẽ chạy trong GitLab CI service containers.)
/// </summary>
public class CatalogDbTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Seed_InsertsSampleProducts()
    {
        using var db = CreateDb();

        await CatalogSeed.SeedAsync(db);

        Assert.True(await db.Set<Product>().CountAsync() >= 8);
        Assert.True(await db.Set<Product>().AnyAsync(p => p.Variants.Count >= 4));
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        using var db = CreateDb();

        await CatalogSeed.SeedAsync(db);
        var countAfterFirst = await db.Set<Product>().CountAsync();
        await CatalogSeed.SeedAsync(db);

        Assert.Equal(countAfterFirst, await db.Set<Product>().CountAsync());
    }

    [Fact]
    public async Task Product_WithVariants_CanBeQueried()
    {
        using var db = CreateDb();
        await CatalogSeed.SeedAsync(db);

        var product = await db.Set<Product>()
            .Where(p => p.SalePrice != null)
            .OrderBy(p => p.Id)
            .FirstAsync();

        Assert.NotNull(product);
        Assert.NotEmpty(product.Slug);
        Assert.All(product.Variants, v => Assert.False(string.IsNullOrEmpty(v.SizeName)));
    }
}
