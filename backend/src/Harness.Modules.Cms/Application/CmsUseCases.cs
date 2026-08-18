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
            .Select(b => new BannerDto(b.Id, b.Title, b.ImageUrl, b.LinkUrl, b.Position, b.SortOrder, true))
            .ToListAsync(cancellationToken);
    }
}

public record BannerDto(int Id, string Title, string ImageUrl, string? LinkUrl, string Position, int SortOrder, bool IsActive);

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

// ===== Quản trị banner =====

public record CreateBannerCommand(string Title, string ImageUrl, string? LinkUrl, string Position, int SortOrder, DateTimeOffset? StartAt = null, DateTimeOffset? EndAt = null) : IRequest<int>;

public class CreateBannerCommandHandler : IRequestHandler<CreateBannerCommand, int>
{
    private readonly IHarnessDbContext _db;
    public CreateBannerCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<int> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = new Banner
        {
            Title = request.Title.Trim(),
            ImageUrl = request.ImageUrl,
            LinkUrl = request.LinkUrl,
            Position = request.Position,
            SortOrder = request.SortOrder,
            IsActive = true,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };
        _db.Set<Banner>().Add(banner);
        await _db.SaveChangesAsync(cancellationToken);
        return banner.Id;
    }
}

public record DeactivateBannerCommand(int Id) : IRequest<bool>;

public class DeactivateBannerCommandHandler : IRequestHandler<DeactivateBannerCommand, bool>
{
    private readonly IHarnessDbContext _db;
    public DeactivateBannerCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<bool> Handle(DeactivateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = await _db.Set<Banner>().FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy banner #{request.Id}.");
        banner.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record GetAllBannersQuery : IRequest<IReadOnlyList<BannerDto>>;

public class GetAllBannersQueryHandler : IRequestHandler<GetAllBannersQuery, IReadOnlyList<BannerDto>>
{
    private readonly IHarnessDbContext _db;
    public GetAllBannersQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<BannerDto>> Handle(GetAllBannersQuery request, CancellationToken cancellationToken)
        => await _db.Set<Banner>().AsNoTracking()
            .OrderBy(b => b.Position).ThenBy(b => b.SortOrder)
            .Select(b => new BannerDto(b.Id, b.Title, b.ImageUrl, b.LinkUrl, b.Position, b.SortOrder, b.IsActive))
            .ToListAsync(cancellationToken);
}