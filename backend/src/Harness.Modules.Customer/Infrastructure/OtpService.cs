using System.Security.Cryptography;
using Harness.BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Harness.Modules.Customer.Infrastructure;

/// <summary>
/// Dịch vụ OTP dựa trên cache (Redis trong Production, in-memory khi dev qua IDistributedCache):
/// sinh mã, lưu theo key `otp:{phone}` có hết hạn; phát session token theo key `session:{token}`.
/// Sandbox chưa nối SMS gateway nên "gửi" bằng log (SmsProvider=log) và có thể trả mã khi debug.
/// </summary>
public class OtpService
{
    private readonly ICacheService _cache;
    private readonly ILogger<OtpService> _logger;
    private readonly OtpOptions _options;

    public OtpService(ICacheService cache, IOptions<OtpOptions> options, ILogger<OtpService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string phone, CancellationToken cancellationToken = default)
    {
        var length = Math.Clamp(_options.CodeLength, 4, 8);
        var max = (int)Math.Pow(10, length);
        var code = RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");

        await _cache.SetAsync($"otp:{phone}", code, TimeSpan.FromMinutes(_options.ExpiryMinutes), cancellationToken);

        _logger.LogInformation(
            "[OTP sandbox] Gửi mã {Code} tới {Phone} (hết hạn sau {Min} phút, kênh: {Provider})",
            code, phone, _options.ExpiryMinutes, _options.SmsProvider);

        return code;
    }

    public async Task<bool> VerifyAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        var stored = await _cache.GetAsync<string>($"otp:{phone}", cancellationToken);
        if (string.IsNullOrEmpty(stored) || !string.Equals(stored, code, StringComparison.Ordinal))
            return false;

        await _cache.RemoveAsync($"otp:{phone}", cancellationToken);
        return true;
    }

    public async Task<string> IssueSessionAsync(string phone, CancellationToken cancellationToken = default)
    {
        var token = Guid.NewGuid().ToString("N");
        await _cache.SetAsync($"session:{token}", phone, TimeSpan.FromHours(_options.SessionTtlHours), cancellationToken);
        return token;
    }

    public async Task<string?> ResolveSessionAsync(string token, CancellationToken cancellationToken = default)
        => await _cache.GetAsync<string>($"session:{token}", cancellationToken);
}
