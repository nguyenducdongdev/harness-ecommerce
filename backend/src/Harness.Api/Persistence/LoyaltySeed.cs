using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Loyalty.Domain;
using Microsoft.EntityFrameworkCore;

namespace Harness.Api.Persistence;

/// <summary>Seed kho quà chương trình tích điểm — chạy khi bảng loyalty_rewards trống (Development).</summary>
public static class LoyaltySeed
{
    public static async Task SeedAsync(IHarnessDbContext db)
    {
        if (await db.Set<Reward>().AnyAsync()) return;

        var rewards = new[]
        {
            Reward.Create("Voucher giảm 100.000đ", 100, 100_000, "Đổi voucher giảm 100.000đ cho đơn từ 1.000.000đ."),
            Reward.Create("Voucher giảm 300.000đ", 300, 300_000, "Đổi voucher giảm 300.000đ cho đơn từ 3.000.000đ."),
            Reward.Create("Túi tote thời trang Harness", 200, 250_000, "Túi vải canvas in logo — sử dụng được trong showroom."),
            Reward.Create("Gối tựa lưng cao cấp", 500, 650_000, "Gối tựa lưng ergonomic cho ghế phòng khách."),
            Reward.Create("Phiếu ưu đãi 5%", 900, 1_000_000, "Giảm 5% tối đa 1.000.000đ cho đơn tiếp theo."),
        };

        foreach (var reward in rewards)
            db.Set<Reward>().Add(reward);

        await db.SaveChangesAsync();
    }
}
