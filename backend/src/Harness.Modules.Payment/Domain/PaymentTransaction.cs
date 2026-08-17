using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Payment.Domain;

public enum PaymentStatus { Pending = 1, Succeeded = 2, Failed = 3, Refunded = 4 }

/// <summary>Giao dịch thanh toán của đơn hàng (VNPay/MoMo/ZaloPay/COD...).</summary>
public class PaymentTransaction : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = default!; // vnpay / momo / zalopay / cod
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? ProviderTransactionRef { get; private set; }
    public string? RawPayload { get; private set; } // jsonb — payload gốc từ webhook
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; private set; }

    private PaymentTransaction() { } // EF

    public static PaymentTransaction Create(Guid orderId, string provider, decimal amount) =>
        new() { Id = Guid.NewGuid(), OrderId = orderId, Provider = provider, Amount = amount };

    public void MarkSucceeded(string? providerRef, string? rawPayload = null)
    {
        EnsurePending();
        Status = PaymentStatus.Succeeded;
        ProviderTransactionRef = providerRef;
        RawPayload = rawPayload;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string? rawPayload = null)
    {
        EnsurePending();
        Status = PaymentStatus.Failed;
        RawPayload = rawPayload;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Giao dịch đã ở trạng thái {Status}, không thể cập nhật.");
    }
}

public sealed record PaymentSucceededIntegrationEvent(Guid OrderId, string Provider, decimal Amount) : IntegrationEvent
{
    public override string EventType => "payment.succeeded";
}
