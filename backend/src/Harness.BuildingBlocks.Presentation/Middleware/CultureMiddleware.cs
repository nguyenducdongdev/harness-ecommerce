using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Harness.BuildingBlocks.Presentation.Middleware;

public class CultureMiddleware
{
    private readonly RequestDelegate _next;

    public CultureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? lang = context.Request.Query["lang"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(lang))
        {
            var header = context.Request.Headers["Accept-Language"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header))
            {
                lang = header.Split(',')[0].Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(lang))
        {
            try
            {
                var cultureName = lang.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "vi-VN";
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // Fallback to default
            }
        }

        await _next(context);
    }
}
