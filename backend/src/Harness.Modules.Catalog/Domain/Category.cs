using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Catalog.Domain;

public class Category : AuditableEntity<int>
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
