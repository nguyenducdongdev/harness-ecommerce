using Harness.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.BuildingBlocks.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class I18nController : ControllerBase
{
    private readonly ICurrencyConverterService _currencyConverter;
    private readonly ILocalizationService _localizationService;

    public I18nController(
        ICurrencyConverterService currencyConverter,
        ILocalizationService localizationService)
    {
        _currencyConverter = currencyConverter;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Lấy danh sách các đồng tiền được hỗ trợ cùng tỷ giá quy đổi sang VND.
    /// </summary>
    [HttpGet("currencies")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CurrencyRate>>), StatusCodes.Status200OK)]
    public IActionResult GetSupportedCurrencies()
    {
        var result = _currencyConverter.GetSupportedCurrencies();
        return Ok(ApiResponse<IReadOnlyList<CurrencyRate>>.Ok(result));
    }

    /// <summary>
    /// Quy đổi số tiền giữa các loại tiền tệ (VD: VND sang USD, EUR sang VND).
    /// </summary>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(ApiResponse<CurrencyConversionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public IActionResult ConvertCurrency(
        [FromQuery] decimal amount,
        [FromQuery] string from = "VND",
        [FromQuery] string to = "USD")
    {
        try
        {
            var result = _currencyConverter.ConvertDetails(amount, from, to);
            return Ok(ApiResponse<CurrencyConversionResult>.Ok(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Lấy danh sách từ điển bản dịch đa ngôn ngữ cho frontend (vi / en).
    /// </summary>
    [HttpGet("translations")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyDictionary<string, string>>), StatusCodes.Status200OK)]
    public IActionResult GetTranslations([FromQuery] string lang = "vi")
    {
        var strings = _localizationService.GetAllStrings(lang);
        return Ok(ApiResponse<IReadOnlyDictionary<string, string>>.Ok(strings));
    }
}
