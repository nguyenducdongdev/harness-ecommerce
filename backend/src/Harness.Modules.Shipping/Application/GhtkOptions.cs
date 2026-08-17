namespace Harness.Modules.Shipping.Application;

public class GhtkOptions
{
    public const string SectionName = "Shipping:GHTK";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://services.giaohangtietkiem.vn";
    public string ApiToken { get; set; } = default!;
}
