using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Cms.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Cms.Application;

public record GetActiveBannersQuery(string Position = "home-hero") : IRequest<IReadOnlyList<BannerDto>>;

public class GetActiveBannersQueryHandler : IRequestHandler<GetActiveBannersQuery, IReadOnlyList<BannerDto>>
{
    private readonly IHarnessDbContext _db;

    public GetActiveBannersQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<BannerDto>> Handle(GetActiveBannersQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.Set<Banner>().AsNoTracking()
            .Where(b => b.IsActive && b.Position == request.Position
                        && (b.StartAt == null || b.StartAt <= now)
                        && (b.EndAt == null || b.EndAt >= now))
            .OrderBy(b => b.SortOrder)
            .Select(b => new BannerDto(b.Id, b.Title, b.ImageUrl, b.LinkUrl, b.Position, b.SortOrder))
            .ToListAsync(cancellationToken);
    }
}

public record BannerDto(int Id, string Title, string ImageUrl, string? LinkUrl, string Position, int SortOrder);

public record GetPageBySlugQuery(string Slug) : IRequest<PageDto?>;

public class GetPageBySlugQueryHandler : IRequestHandler<GetPageBySlugQuery, PageDto?>
{
    private readonly IHarnessDbContext _db;

    public GetPageBySlugQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PageDto?> Handle(GetPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var page = await _db.Set<Page>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug && p.IsPublished, cancellationToken);
        return page is null
            ? null
            : new PageDto(page.Id, page.Title, page.Slug, page.HtmlContent, page.MetaTitle, page.MetaDescription);
    }
}

public record PageDto(int Id, string Title, string Slug, string HtmlContent, string? MetaTitle, string? MetaDescription);
