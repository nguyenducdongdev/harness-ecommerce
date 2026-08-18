namespace Harness.Modules.Integration.Infrastructure;

/// <summary>Cấu hình đồng bộ ERP/DMS — section "Integration:Erp".</summary>
public class ErpOptions
{
    public const string SectionName = "Integration:Erp";

    /// <summary>Bật consumer ERP (dev/prod có RabbitMQ). Tắt khi chạy nhiều instance API để tránh consume trùng.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Tên queue consume (mỗi instance nên dùng tên riêng nếu chạy song song).</summary>
    public string QueueName { get; set; } = "harness.erp-sync";
    public int MaxRetry { get; set; } = 3;
}