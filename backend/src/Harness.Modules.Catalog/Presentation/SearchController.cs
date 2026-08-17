using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Catalog.Application.Abstractions;
using Harness.Modules.Catalog.Infrastructure.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Catalog.Presentation;

/// <summary>Tìm kiếm sản phẩm full-text trên Elasticsearch + quản lý chỉ mục.</summary>
public class SearchController : ApiController
{
    public SearchController(ISender mediator) : base(mediator) { }

    /// <summary>Tìm kiếm sản phẩm theo từ khóa (name/description/attributes).</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductSearchDocument>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Products(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IProductSearch search,
        CancellationToken cancellationToken)
    {
        var size = Math.Clamp(pageSize, 1, 100);
        var from = Math.Max(page - 1, 0) * size;
        var results = await search.SearchProductsAsync(q ?? string.Empty, from, size, cancellationToken);
        return Ok(ApiResponse.Ok(results));
    }

    /// <summary>Build lại toàn bộ chỉ mục sản phẩm từ DB (admin).</summary>
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex([FromServices] ProductReindexService service, CancellationToken cancellationToken)
    {
        var count = await service.ReindexAllAsync(cancellationToken);
        return Ok(ApiResponse.Ok(new { indexed = count }, $"Đã lập chỉ mục {count} sản phẩm."));
    }
}
