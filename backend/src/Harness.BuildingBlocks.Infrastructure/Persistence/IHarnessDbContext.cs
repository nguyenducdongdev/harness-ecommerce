namespace Harness.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Abstraction DbContext dùng chung cho handler các module — tránh phụ thuộc vòng:
/// module → interface này, còn AppDbContext thật định nghĩa tại composition root (Harness.Api).
/// </summary>
public interface IHarnessDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
