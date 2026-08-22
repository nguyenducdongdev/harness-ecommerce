using Harness.BuildingBlocks.Infrastructure.Localization;
using Xunit;

namespace Harness.UnitTests;

public class I18nTests
{
    private readonly CurrencyConverterService _currencyConverter = new();
    private readonly LocalizationService _localizationService = new();

    [Fact]
    public void GetSupportedCurrencies_ShouldReturnVndUsdEur()
    {
        var list = _currencyConverter.GetSupportedCurrencies();

        Assert.Equal(3, list.Count);
        Assert.Contains(list, c => c.Code == "VND");
        Assert.Contains(list, c => c.Code == "USD");
        Assert.Contains(list, c => c.Code == "EUR");
    }

    [Fact]
    public void Convert_VND_To_USD_ShouldCalculateCorrectly()
    {
        // 25,400,000 VND -> 1,000 USD (at 1 USD = 25,400 VND)
        decimal converted = _currencyConverter.Convert(25400000m, "VND", "USD");

        Assert.Equal(1000.00m, converted);
    }

    [Fact]
    public void Convert_USD_To_VND_ShouldCalculateCorrectly()
    {
        // 100 USD -> 2,540,000 VND
        decimal converted = _currencyConverter.Convert(100m, "USD", "VND");

        Assert.Equal(2540000m, converted);
    }

    [Fact]
    public void ConvertDetails_ShouldReturnFormattedResult()
    {
        var result = _currencyConverter.ConvertDetails(10000000m, "VND", "USD");

        Assert.Equal(10000000m, result.OriginalAmount);
        Assert.Equal("VND", result.FromCurrency);
        Assert.Equal("USD", result.ToCurrency);
        Assert.True(result.ConvertedAmount > 0);
        Assert.StartsWith("$", result.Formatted);
    }

    [Fact]
    public void Format_ShouldFormatVndAndUsdCorrectly()
    {
        string formattedVnd = _currencyConverter.Format(1500000m, "VND");
        string formattedUsd = _currencyConverter.Format(150.5m, "USD");

        Assert.Contains("₫", formattedVnd);
        Assert.Equal("$150.50", formattedUsd);
    }

    [Fact]
    public void GetString_ShouldReturnLocalizedValues()
    {
        string viWelcome = _localizationService.GetString("Welcome", "vi");
        string enWelcome = _localizationService.GetString("Welcome", "en");

        Assert.Contains("Chào mừng", viWelcome);
        Assert.Contains("Welcome", enWelcome);
    }

    [Fact]
    public void GetString_MissingKey_ShouldFallbackToKey()
    {
        string result = _localizationService.GetString("NonExistentKey", "en");

        Assert.Equal("NonExistentKey", result);
    }
}
