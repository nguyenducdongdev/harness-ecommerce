using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Commands;
using Harness.Modules.Catalog.Application.Dtos;
using Harness.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Catalog.Application.Queries;

public record SearchProductsQuery(
    int Page = 1,
    int PageSize = 20,
    string? CategorySlug = null,
    string? SearchTerm = null,
    int? BrandId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string Sort = ProductSort.Newest,
    bool OnlyActive = true) : IRequest<PagedResult<ProductDto>>;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PagedResult<ProductDto>>
{
    private readonly IHarnessDbContext _db;

    public SearchProductsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = from p in _db.Set<Product>().AsNoTracking()
                    join c in _db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
                    join b in _db.Set<Brand>().AsNoTracking() on p.BrandId equals b.Id
                    select new { p, c, b };

        if (request.OnlyActive) query = query.Where(x => x.p.IsActive);
        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
            query = query.Where(x => x.c.Slug == request.CategorySlug);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(x => x.p.Name.ToLower().Contains(term) || x.p.Sku.ToLower().Contains(term));
        }
        if (request.BrandId.HasValue) query = query.Where(x => x.p.BrandId == request.BrandId);
        if (request.MinPrice.HasValue) query = query.Where(x => x.Price >= request.MinPrice);
        if (request.MaxPrice.HasValue) query = query.Where(x => x.Price <= request.MaxPrice);

        query = request.Sort switch
        {
            ProductSort.PriceAsc => query.OrderBy(x => x.p.SalePrice ?? x.p.Price),
            ProductSort.PriceDesc => query.OrderByDescending(x => x.p.SalePrice ?? x.p.Price),
            ProductSort.Popular => query.OrderByDescending(x => x.p.ViewCount),
            ProductSort.BestSelling => query.OrderByDescending(x => x.p.Id), // TODO Phase 2: thay bằng số bán thực tế từ Order module
            _ => query.OrderByDescending(x => x.p.CreatedAt)
        };

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => ProductMapper.ToDto(x.p, x.c.Name, x.b.Name, x.c.Slug)).ToList();
        return PagedResult<ProductDto>.Create(dtos, total, page, pageSize);
    }
}

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDto?>;

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ProductDto?>
{
    private readonly IHarnessDbContext _db;

    public GetProductBySlugQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<ProductDto?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var row = await (
            from p in _db.Set<Product>().AsNoTracking()
            join c in _db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            join b in _db.Set<Brand>().AsNoTracking() on p.BrandId equals b.Id
            where p.Slug == request.Slug && p.IsActive
            select new { p, c, b }).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ProductMapper.ToDto(row.p, row.c.Name, row.b.Name, row.c.Slug);
    }
}

public record GetCategoriesQuery(bool OnlyActive = true) : IRequest<IReadOnlyList<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly IHarnessDbContext _db;

    public GetCategoriesQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<Category>().AsNoTracking().AsEnumerable();
        if (request.OnlyActive) query = query.Where(c => c.IsActive);

        return query.OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ParentId, c.SortOrder, c.IsActive))
            .ToList();
    }
}

public record GetFeaturedProductsQuery(int Take = 8) : IRequest<IReadOnlyList<ProductDto>>;

public class GetFeaturedProductsQueryHandler : IRequestHandler<GetFeaturedProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IHarnessDbContext _db;

    public GetFeaturedProductsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        var rows = await (
            from p in _db.Set<Product>().AsNoTracking()
            join c in _db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
            join b in _db.Set<Brand>().AsNoTracking() on p.BrandId equals b.Id
            where p.IsActive && p.IsFeatured
            orderby p.CreatedAt descending
            select new { p, c, b }).Take(request.Take).ToListAsync(cancellationToken);

        return rows.Select(x => ProductMapper.ToDto(x.p, x.c.Name, x.b.Name, x.c.Slug)).ToList();
    }
}
