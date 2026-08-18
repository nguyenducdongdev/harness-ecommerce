namespace Harness.Modules.Auth.Infrastructure;

/// <summary>Cấu hình JWT — section "Auth" trong appsettings.</summary>
public class JwtOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "harness-api";
    public string Audience { get; set; } = "harness-admin";
    /// <summary>Khóa bí mật — tối thiểu 32 ký tự trong Production (đặt qua env Auth__SecretKey).</summary>
    public string SecretKey { get; set; } = "harness-dev-secret-key-change-me-0123456789";
    public int ExpiryHours { get; set; } = 8;
    /// <summary>Mật khẩu mặc định cho tài khoản admin seed (chỉ dev — đổi ngay khi lên production).</summary>
    public string DefaultAdminPassword { get; set; } = "Harness@123";
    public string DefaultAdminUsername { get; set; } = "admin";
    /// <summary>Bật/tắt seed tài khoản admin mặc định (Production nên đặt false qua Auth__Seed).</summary>
    public bool Seed { get; set; } = true;
}