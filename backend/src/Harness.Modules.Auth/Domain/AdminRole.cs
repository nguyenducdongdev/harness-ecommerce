using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Auth.Domain;

/// <summary>Vai trò admin hệ thống (rbac).</summary>
public class AdminRole : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; } = true;

    private AdminRole() { }

    public static AdminRole Create(string name, string? description = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Description = description,
        IsSystem = true
    };

    public ICollection<AdminUserRole> UserRoles { get; private set; } = new List<AdminUserRole>();
}

/// <summary>Danh sách vai trò mặc định hệ thống.</summary>
public static class AdminRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Operations = "Operations";
    public const string Warehouse = "Warehouse";
    public const string Content = "Content";
    public const string Reviewer = "Reviewer";

    public static readonly string[] All =
    {
        SuperAdmin, Admin, Operations, Warehouse, Content, Reviewer
    };
}