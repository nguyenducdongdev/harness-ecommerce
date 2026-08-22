using System.Globalization;
using Harness.BuildingBlocks.Application.Abstractions;

namespace Harness.BuildingBlocks.Infrastructure.Localization;

public class CurrencyConverterService : ICurrencyConverterService
{
    private static readonly Dictionary<string, CurrencyRate> Currencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VND"] = new CurrencyRate("VND", "Vietnamese Dong", "₫", 1.0m),
        ["USD"] = new CurrencyRate("USD", "US Dollar", "$", 25400.0m),
        ["EUR"] = new CurrencyRate("EUR", "Euro", "€", 27500.0m)
    };

    public IReadOnlyList<CurrencyRate> GetSupportedCurrencies()
    {
        return Currencies.Values.ToList().AsReadOnly();
    }

    public decimal Convert(decimal amount, string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        if (!Currencies.TryGetValue(fromCurrency, out var fromRate))
            throw new ArgumentException($"Unsupported source currency: {fromCurrency}");

        if (!Currencies.TryGetValue(toCurrency, out var toRate))
            throw new ArgumentException($"Unsupported target currency: {toCurrency}");

        // Convert from source currency to VND base, then to target currency
        decimal amountInVnd = amount * fromRate.ExchangeRateToVnd;
        decimal convertedAmount = amountInVnd / toRate.ExchangeRateToVnd;

        // VND has 0 decimals, USD/EUR have 2 decimals
        return string.Equals(toCurrency, "VND", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(convertedAmount, 0, MidpointRounding.AwayFromZero)
            : Math.Round(convertedAmount, 2, MidpointRounding.AwayFromZero);
    }

    public CurrencyConversionResult ConvertDetails(decimal amount, string fromCurrency, string toCurrency)
    {
        string from = fromCurrency.ToUpperInvariant();
        string to = toCurrency.ToUpperInvariant();

        decimal converted = Convert(amount, from, to);

        if (!Currencies.TryGetValue(from, out var fromRate) || !Currencies.TryGetValue(to, out var toRate))
        {
            throw new ArgumentException("Unsupported currency specified.");
        }

        decimal effectiveRate = fromRate.ExchangeRateToVnd / toRate.ExchangeRateToVnd;
        string formatted = Format(converted, to);

        return new CurrencyConversionResult(
            amount,
            from,
            converted,
            to,
            Math.Round(effectiveRate, 6),
            formatted
        );
    }

    public string Format(decimal amount, string currencyCode)
    {
        string code = currencyCode.ToUpperInvariant();
        return code switch
        {
            "USD" => $"${amount:N2}",
            "EUR" => $"€{amount:N2}",
            "VND" => $"{amount:N0} ₫",
            _ => $"{amount:N2} {code}"
        };
    }
}
