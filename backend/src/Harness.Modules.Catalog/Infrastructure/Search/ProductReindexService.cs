using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Catalog.Infrastructure.Search;

/// <summary>Đọc sản phẩm từ DB và đồng bộ lên chỉ mục Elasticsearch.</summary>
public class ProductReindexService
{
    private readonly IHarnessDbContext _db;
    private readonly IProductIndexer _indexer;

    public ProductReindexService(IHarnessDbContext db, IProductIndexer indexer)
    {
        _db = db;
        _indexer = indexer;
    }

    /// <summary>Build toàn bộ chỉ mục từ các product đang hoạt động. Trả về số lượng đã index.</summary>
    public async Task<int> ReindexAllAsync(CancellationToken cancellationToken = default)
    {
        await _indexer.EnsureIndexAsync(cancellationToken);

        var rows = await (
            from p in _db.Set<Product>().AsNoTracking()
            join c in _db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            join b in _db.Set<Brand>().AsNoTracking() on p.BrandId equals b.Id
            where p.IsActive
            select new { p, c, b }).ToListAsync(cancellationToken);

        var docs = rows
            .Select(r => ProductSearchDocument.FromProduct(r.p, r.c.Name, r.c.Slug, r.b.Name))
            .ToList();

        await _indexer.IndexManyAsync(docs, cancellationToken);
        return docs.Count;
    }

    /// <summary>Index riêng một sản phẩm (gọi sau khi tạo/cập nhật).</summary>
    public async Task IndexProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from p in _db.Set<Product>().AsNoTracking()
            join c in _db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            join b in _db.Set<Brand>().AsNoTracking() on p.BrandId equals b.Id
            where p.Id == productId
            select new { p, c, b }).FirstOrDefaultAsync(cancellationToken);

        if (row is null) return;

        await _indexer.IndexProductAsync(
            ProductSearchDocument.FromProduct(row.p, row.c.Name, row.c.Slug, row.b.Name), cancellationToken);
    }
}
