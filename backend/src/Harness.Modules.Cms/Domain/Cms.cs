using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Cms.Domain;

/// <summary>Banner quảng cáo theo vị trí trang chủ.</summary>
public class Banner : AuditableEntity<int>
{
    public string Title { get; set; } = default!;
    public string ImageUrl { get; set; } = default!;
    public string? LinkUrl { get; set; }
    /// <summary>Vị trí: home-hero, home-mid, category-top...</summary>
    public string Position { get; set; } = "home-hero";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
}

/// <summary>Trang nội dung tĩnh (giới thiệu, chính sách, hướng dẫn chọn nội thất...).</summary>
public class Page : AuditableEntity<int>
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string HtmlContent { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; }
}
