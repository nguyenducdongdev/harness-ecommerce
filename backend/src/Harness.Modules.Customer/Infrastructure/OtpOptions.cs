namespace Harness.Modules.Customer.Infrastructure;

public class OtpOptions
{
    public const string SectionName = "Otp";
    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 5;
    public int SessionTtlHours { get; set; } = 24;
    /// <summary>Sandbox không có SMS thật → trả mã OTP trong response để test (Production bật false).</summary>
    public bool ReturnCodeInResponse { get; set; } = true;
    public string SmsProvider { get; set; } = "log"; // log | (Phase 3: bật SMS gateway thật)
}
