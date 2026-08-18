using Microsoft.Extensions.Options;

namespace Harness.Api.Observability;

public class MetricsOptions
{
    public const string SectionName = "Metrics";
    public bool Enabled { get; set; } = true;
    public int ReportIntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Background service: định kỳ cập nhật business metrics (outbox, ERP sync, products indexed)
/// để /metrics luôn phản ánh trạng thái hiện tại. Không làm chết app nếu một lần chạy lỗi.
/// </summary>
public class MetricsReporterHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MetricsOptions _options;
    private readonly ILogger<MetricsReporterHostedService> _logger;

    public MetricsReporterHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<MetricsOptions> options,
        ILogger<MetricsReporterHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Metrics reporter tắt (Metrics:Enabled=false).");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.ReportIntervalSeconds));
        _logger.LogInformation("Metrics reporter khởi động — cập nhật mỗi {Interval}s.", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reporter = scope.ServiceProvider.GetRequiredService<MetricsReporter>();
                await reporter.ReportAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown bình thường
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Metrics reporter bỏ qua lần chạy này (không ảnh hưởng ứng dụng).");
            }
        }
    }
}
