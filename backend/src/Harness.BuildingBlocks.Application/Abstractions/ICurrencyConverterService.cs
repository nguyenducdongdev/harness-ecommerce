namespace Harness.BuildingBlocks.Application.Abstractions;

public record CurrencyRate(string Code, string Name, string Symbol, decimal ExchangeRateToVnd);

public record CurrencyConversionResult(
    decimal OriginalAmount,
    string FromCurrency,
    decimal ConvertedAmount,
    string ToCurrency,
    decimal ExchangeRate,
    string Formatted
);

public interface ICurrencyConverterService
{
    IReadOnlyList<CurrencyRate> GetSupportedCurrencies();
    decimal Convert(decimal amount, string fromCurrency, string toCurrency);
    CurrencyConversionResult ConvertDetails(decimal amount, string fromCurrency, string toCurrency);
    string Format(decimal amount, string currencyCode);
}
