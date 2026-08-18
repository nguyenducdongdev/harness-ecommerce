using Harness.Api.Persistence;
using Harness.Modules.Auth.Application;
using Harness.Modules.Auth.Domain;
using Harness.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Harness.IntegrationTests;

/// <summary>Test auth admin: seed, login, JWT, RBAC handler.</summary>
public class AuthTests
{
    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static JwtTokenService Jwt() =>
        new(Options.Create(new JwtOptions { SecretKey = "0123456789abcdef0123456789abcdef0123456789" }));

    private static async Task<AppDbContext> SeedAdminAsync(AppDbContext db, params string[] roles)
    {
        var created = new List<AdminRole>();
        foreach (var roleName in roles)
        {
            var existing = await db.Set<AdminRole>().FirstOrDefaultAsync(r => r.Name == roleName);
            if (existing is null)
            {
                var role = db.Set<AdminRole>().Add(AdminRole.Create(roleName));
                created.Add(role.Entity);
            }
            else
            {
                created.Add(existing);
            }
        }
        await db.SaveChangesAsync();

        var user = AdminUser.Create("admin", PasswordHashHelper.Hash("Harness@123"), "Quản trị viên");
        user.AssignRoles(created);
        db.Set<AdminUser>().Add(user);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task AdminLogin_ValidCredentials_ReturnsJwtWithRoles()
    {
        using var db = await SeedAdminAsync(CreateDb(), AdminRoles.Admin, AdminRoles.SuperAdmin);

        var result = await new AdminLoginCommandHandler(db, Jwt())
            .Handle(new AdminLoginCommand("admin", "Harness@123"), CancellationToken.None);

        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("admin", result.Username);
        Assert.Contains(AdminRoles.Admin, result.Roles);
        Assert.Contains(AdminRoles.SuperAdmin, result.Roles);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AdminLogin_WrongPassword_Throws()
    {
        using var db = await SeedAdminAsync(CreateDb(), AdminRoles.Admin);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new AdminLoginCommandHandler(db, Jwt())
                .Handle(new AdminLoginCommand("admin", "wrong"), CancellationToken.None));
    }

    [Fact]
    public async Task AdminLogin_NoRoles_Throws()
    {
        using var db = await SeedAdminAsync(CreateDb());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new AdminLoginCommandHandler(db, Jwt())
                .Handle(new AdminLoginCommand("admin", "Harness@123"), CancellationToken.None));
    }

    [Fact]
    public async Task AdminLogin_InactiveUser_Throws()
    {
        using var db = CreateDb();
        var admin = db.Set<AdminRole>().Add(AdminRole.Create(AdminRoles.Admin));
        await db.SaveChangesAsync();
        var user = AdminUser.Create("off", PasswordHashHelper.Hash("Harness@123"), "Off");
        user.AssignRoles(new[] { admin.Entity });
        user.Deactivate();
        db.Set<AdminUser>().Add(user);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new AdminLoginCommandHandler(db, Jwt())
                .Handle(new AdminLoginCommand("off", "Harness@123"), CancellationToken.None));
    }

    [Fact]
    public async Task AuthSeed_SeedsRolesAndAdmin()
    {
        using var db = CreateDb();

        await AuthSeed.SeedAsync(db, new JwtOptions { Seed = true });

        Assert.Equal(AdminRoles.All.Length, await db.Set<AdminRole>().CountAsync());
        var admin = await db.Set<AdminUser>()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Username == "admin");
        Assert.True(PasswordHashHelper.Verify("Harness@123", admin.PasswordHash));
        Assert.Contains(admin.UserRoles, ur => ur.Role.Name == AdminRoles.Admin);
    }
}