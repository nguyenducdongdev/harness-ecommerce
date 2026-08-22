namespace Harness.BuildingBlocks.Application.Abstractions;

public interface ILocalizationService
{
    string GetString(string key, string culture = "vi");
    IReadOnlyDictionary<string, string> GetAllStrings(string culture = "vi");
}
