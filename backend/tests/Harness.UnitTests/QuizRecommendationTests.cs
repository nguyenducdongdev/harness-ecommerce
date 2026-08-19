using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Queries;
using Harness.Modules.Catalog.Domain;
using Harness.Modules.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Harness.UnitTests;

public class TestCatalogDbContext : DbContext, IHarnessDbContext
{
    public TestCatalogDbContext(DbContextOptions<TestCatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CategoryConfiguration).Assembly);
    }
}

public class QuizRecommendationTests
{
    private static TestCatalogDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestCatalogDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new TestCatalogDbContext(options);
    }

    [Fact]
    public async Task GetQuizRecommendation_ShouldReturnMatchedProductsAndSummary()
    {
        using var db = CreateDbContext(nameof(GetQuizRecommendation_ShouldReturnMatchedProductsAndSummary));

        var cat = new Category { Name = "Sofa", Slug = "sofa" };
        var brand = new Brand { Name = "Việt Nam", Slug = "viet-nam" };
        db.Set<Category>().Add(cat);
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync();

        var p1 = Product.Create("Sofa Băng Da", "sofa-bang-da", "SKU-SOFA1", cat.Id, brand.Id, 15_000_000, null, 12, "Sofa đẹp", "Chi tiết",
            new Dictionary<string, string> { { "phong-cach", "Scandinavian" } }, null, true);
        p1.SetModel3dUrl("/models/sofa.gltf");

        var p2 = Product.Create("Bàn Trà Gỗ", "ban-tra-go", "SKU-BANTRA", cat.Id, brand.Id, 5_000_000, null, 12, "Bàn trà", "Chi tiết",
            new Dictionary<string, string> { { "phong-cach", "Modern" } });

        db.Set<Product>().AddRange(p1, p2);
        await db.SaveChangesAsync();

        var handler = new GetQuizRecommendationQueryHandler(db);
        var req = new QuizRequestDto("phong-khach", 25, "Scandinavian", 1_000_000, 30_000_000);

        var result = await handler.Handle(new GetQuizRecommendationQuery(req), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("phong-khach", result.RoomType);
        Assert.Equal("Scandinavian", result.Style);
        Assert.NotEmpty(result.Summary);
        Assert.NotEmpty(result.RecommendedProducts);
        Assert.Equal("/models/sofa.gltf", result.RecommendedProducts[0].Model3dUrl);
    }
}


