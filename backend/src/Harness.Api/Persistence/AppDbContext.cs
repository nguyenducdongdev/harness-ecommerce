using Harness.BuildingBlocks.Domain;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Harness.Api.Persistence;

/// <summary>
/// Composition root DbContext: gom model của tất cả module (mỗi module tự cấu hình
/// qua IEntityTypeConfiguration trong assembly của nó — multi-schema PostgreSQL).
/// Khi tách microservice sau này, migrate schema tương ứng sang DB riêng của service.
/// </summary>
public class AppDbContext : DbContext, IHarnessDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Catalog.Infrastructure.Persistence.CatalogConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Order.Infrastructure.Persistence.OrderConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Inventory.Infrastructure.Persistence.InventoryConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Customer.Infrastructure.Persistence.CustomerConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Promotion.Infrastructure.Persistence.PromotionConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Payment.Infrastructure.Persistence.PaymentConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Shipping.Infrastructure.Persistence.ShippingConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Loyalty.Infrastructure.Persistence.LoyaltyConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Review.Infrastructure.Persistence.ReviewConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Cms.Infrastructure.Persistence.CmsConfigurations).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Harness.Modules.Integration.Infrastructure.Persistence.IntegrationConfigurations).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity<Guid>>())
            SetAuditFields(entry.Entity, entry.State.ToString(), now);
        foreach (var entry in ChangeTracker.Entries<AuditableEntity<int>>())
            SetAuditFields(entry.Entity, entry.State.ToString(), now);
        return base.SaveChangesAsync(cancellationToken);
    }

    private static void SetAuditFields<T>(AuditableEntity<T> entity, string state, DateTimeOffset now)
    {
        if (state == "Added") entity.CreatedAt = now;
        else if (state == "Modified") entity.ModifiedAt = now;
    }
}
