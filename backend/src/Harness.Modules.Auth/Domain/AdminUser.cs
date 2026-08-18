using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Auth.Domain;

/// <summary>Tài khoản quản trị nội bộ (dashboard admin / vận hành).</summary>
public class AdminUser : Entity<Guid>
{
    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastLoginAt { get; private set; }

    private AdminUser() { }

    public static AdminUser Create(string username, string passwordHash, string displayName)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username không được để trống.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Mật khẩu chưa được hash.", nameof(passwordHash));

        return new AdminUser
        {
            Id = Guid.NewGuid(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim(),
            IsActive = true
        };
    }

    public void MarkLoggedIn() => LastLoginAt = DateTimeOffset.UtcNow;

    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Mật khẩu chưa được hash.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>Liên kết vai trò (chuẩn hóa theo tên, tránh trùng).</summary>
    public void AssignRoles(IEnumerable<AdminRole> roles)
    {
        var set = new HashSet<AdminRole>(roles);
        UserRoles.Clear();
        foreach (var role in set)
            UserRoles.Add(new AdminUserRole { AdminUserId = Id, AdminRoleId = role.Id });
    }

    public ICollection<AdminUserRole> UserRoles { get; private set; } = new List<AdminUserRole>();
}

/// <summary>Bảng liên kết many-to-many admin_user ↔ admin_role.</summary>
public class AdminUserRole
{
    public Guid AdminUserId { get; set; }
    public Guid AdminRoleId { get; set; }
    public AdminUser User { get; set; } = default!;
    public AdminRole Role { get; set; } = default!;
}