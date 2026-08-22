using Harness.BuildingBlocks.Application.Abstractions;

namespace Harness.BuildingBlocks.Infrastructure.Localization;

public class LocalizationService : ILocalizationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["vi"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Welcome"] = "Chào mừng bạn đến với Harness Ecommerce - Nội thất cao cấp",
            ["OrderCreated"] = "Đơn hàng đã được tạo thành công",
            ["ProductNotFound"] = "Không tìm thấy sản phẩm yêu cầu",
            ["InsufficientStock"] = "Số lượng tồn kho không đủ để đáp ứng đơn hàng",
            ["PaymentSuccess"] = "Thanh toán đơn hàng thành công",
            ["PaymentFailed"] = "Thanh toán không thành công, vui lòng thử lại",
            ["ShippingAddressInvalid"] = "Địa chỉ giao hàng không hợp lệ",
            ["CartEmpty"] = "Giỏ hàng của bạn hiện đang trống",
            ["ExportFurnitureNotice"] = "Sản phẩm đóng gói theo tiêu chuẩn xuất khẩu quốc tế",
            ["ShowroomAllocationNearest"] = "Đã phân bổ kho gần nhất cho địa chỉ giao hàng",
            ["CurrencyUSD"] = "Đô la Mỹ (USD)",
            ["CurrencyVND"] = "Việt Nam Đồng (VND)",
            ["CurrencyEUR"] = "Euro (EUR)",
            ["LanguageVI"] = "Tiếng Việt",
            ["LanguageEN"] = "Tiếng Anh"
        },
        ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Welcome"] = "Welcome to Harness Ecommerce - Premium Furniture",
            ["OrderCreated"] = "Order has been created successfully",
            ["ProductNotFound"] = "The requested product was not found",
            ["InsufficientStock"] = "Insufficient inventory stock to fulfill order",
            ["PaymentSuccess"] = "Payment processed successfully",
            ["PaymentFailed"] = "Payment failed, please try again",
            ["ShippingAddressInvalid"] = "Invalid shipping address",
            ["CartEmpty"] = "Your cart is currently empty",
            ["ExportFurnitureNotice"] = "Products packaged according to international export standards",
            ["ShowroomAllocationNearest"] = "Nearest showroom allocated for your delivery address",
            ["CurrencyUSD"] = "US Dollar (USD)",
            ["CurrencyVND"] = "Vietnamese Dong (VND)",
            ["CurrencyEUR"] = "Euro (EUR)",
            ["LanguageVI"] = "Vietnamese",
            ["LanguageEN"] = "English"
        }
    };

    public string GetString(string key, string culture = "vi")
    {
        string normalizedCulture = NormalizeCulture(culture);

        if (Translations.TryGetValue(normalizedCulture, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }

        // Fallback to 'vi'
        if (Translations["vi"].TryGetValue(key, out var fallbackVal))
        {
            return fallbackVal;
        }

        return key;
    }

    public IReadOnlyDictionary<string, string> GetAllStrings(string culture = "vi")
    {
        string normalizedCulture = NormalizeCulture(culture);
        if (Translations.TryGetValue(normalizedCulture, out var dict))
        {
            return dict;
        }

        return Translations["vi"];
    }

    private static string NormalizeCulture(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return "vi";
        string lower = culture.ToLowerInvariant();
        if (lower.StartsWith("en")) return "en";
        return "vi";
    }
}
