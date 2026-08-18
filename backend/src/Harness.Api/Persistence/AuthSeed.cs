using Harness.Modules.Auth.Domain;
using Harness.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Harness.Api.Persistence;

/// <summary>
/// Seed vai trò admin + tài khoản admin mặc định (dev). Mật khẩu mặc định trong Auth:DefaultAdminPassword
/// — Production phải đổi ngay sau lần đăng nhập đầu tiên (tắt seed qua Auth:Seed = false).
/// </summary>
public static class AuthSeed
{
    public static async Task SeedAsync(AppDbContext db, JwtOptions options)
    {
        if (!options.Seed)
            return;

        // Vai trò mặc định
        var roleDefinitions = new (string Name, string Description)[]
        {
            (AdminRoles.SuperAdmin, "Toàn quyền hệ thống"),
            (AdminRoles.Admin, "Quản trị vận hành"),
            (AdminRoles.Operations, "Xử lý đơn hàng / vận hành"),
            (AdminRoles.Warehouse, "Kho & showroom"),
            (AdminRoles.Content, "Nội dung / banner"),
            (AdminRoles.Reviewer, "Kiểm duyệt đánh giá"),
        };

        var roles = new Dictionary<string, AdminRole>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, description) in roleDefinitions)
        {
            var role = await db.Set<AdminRole>().FirstOrDefaultAsync(r => r.Name == name);
            if (role is null)
            {
                role = AdminRole.Create(name, description);
                db.Set<AdminRole>().Add(role);
            }
            roles[name] = role;
        }
        await db.SaveChangesAsync();

        // Tài khoản admin mặc định (chỉ seed khi chưa có bất kỳ admin nào)
        if (await db.Set<AdminUser>().AnyAsync())
            return;

        var user = AdminUser.Create(
            string.IsNullOrWhiteSpace(options.DefaultAdminUsername) ? "admin" : options.DefaultAdminUsername,
            PasswordHashHelper.Hash(string.IsNullOrWhiteSpace(options.DefaultAdminPassword) ? "Harness@123" : options.DefaultAdminPassword),
            "Quản trị viên");
        user.AssignRoles(await db.Set<AdminRole>()
            .Where(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Admin)
            .ToListAsync());
        db.Set<AdminUser>().Add(user);
        await db.SaveChangesAsync();
    }
}