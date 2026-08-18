using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Cms.Domain;
using Microsoft.EntityFrameworkCore;

namespace Harness.Api.Persistence;

/// <summary>Seed banner CMS — chạy khi bảng banners trống (Development).</summary>
public static class CmsSeed
{
    public static async Task SeedAsync(IHarnessDbContext db)
    {
        if (await db.Set<Banner>().AnyAsync()) return;

        var banners = new[]
        {
            new Banner { Title = "Combo Phòng khách tiết kiệm", ImageUrl = "", LinkUrl = "/products?category=phong-khach", Position = "home-hero", SortOrder = 1 },
            new Banner { Title = "Miễn phí lắp đặt & đo đạc tận nhà", ImageUrl = "", LinkUrl = "/booking", Position = "home-hero", SortOrder = 2 },
            new Banner { Title = "Nội thất thông minh — nhà hiện đại", ImageUrl = "", LinkUrl = "/products?category=noi-that-thong-minh", Position = "home-mid", SortOrder = 1 },
        };

        foreach (var banner in banners)
            db.Set<Banner>().Add(banner);

        await db.SaveChangesAsync();
    }
}
