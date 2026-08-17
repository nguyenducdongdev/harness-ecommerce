using Harness.Modules.Shipping.Domain;

namespace Harness.Modules.Shipping.Application.Providers;

/// <summary>Yêu cầu tính phí vận chuyển hàng cồng kềnh (đơn vị cm / kg).</summary>
public sealed record ShippingFeeRequest(
    int ToDistrictId,
    string ToWardCode,
    int WeightKg,
    int LengthCm,
    int WidthCm,
    int HeightCm,
    int? InsuranceValue = null,
    int? CodValue = null);

/// <summary>Kết quả tính phí từ nhà vận chuyển.</summary>
public sealed record ShippingFeeResult(
    Carrier Carrier,
    decimal Fee,
    string EstimatedDays,
    bool Success,
    string Message,
    string? RawResponse = null);

public interface IShippingProvider
{
    Carrier Carrier { get; }
    Task<ShippingFeeResult> CalculateAsync(ShippingFeeRequest request, CancellationToken cancellationToken = default);
}
