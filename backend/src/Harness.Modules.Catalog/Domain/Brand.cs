using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Catalog.Domain;

public class Brand : AuditableEntity<int>
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? OriginCountry { get; set; }
}
