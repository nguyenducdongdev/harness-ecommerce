using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Catalog.Application.Commands;
using Harness.Modules.Catalog.Application.Dtos;
using Harness.Modules.Catalog.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Catalog.Presentation;

/// <summary>API sản phẩm công khai + quản trị.</summary>
public class ProductsController : ApiController
{
    public ProductsController(ISender mediator) : base(mediator) { }

    /// <summary>Tìm kiếm sản phẩm có phân trang + bộ lọc.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchProductsQuery query)
        => Ok(ApiResponse.Ok(await Mediator.Send(query)));

    /// <summary>Chi tiết sản phẩm theo slug.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await Mediator.Send(new GetProductBySlugQuery(slug));
        return product is null
            ? NotFound(ApiResponse.Fail($"Không tìm thấy sản phẩm '{slug}'."))
            : Ok(ApiResponse.Ok(product));
    }

    /// <summary>Sản phẩm nổi bật trang chủ.</summary>
    [HttpGet("featured")]
    public async Task<IActionResult> Featured([FromQuery] int take = 8)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetFeaturedProductsQuery(take))));

    /// <summary>Tạo sản phẩm mới (admin — sẽ chặn quyền ở Phase 2).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var product = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetBySlug), new { slug = product.Slug }, ApiResponse.Ok(product, "Đã tạo sản phẩm."));
    }

    /// <summary>Cập nhật giá sản phẩm (admin).</summary>
    [HttpPut("{id:int}/price")]
    public async Task<IActionResult> UpdatePrice(int id, [FromBody] UpdateProductPriceRequest request)
    {
        await Mediator.Send(new UpdateProductPriceCommand(id, request.Price, request.SalePrice));
        return Ok(ApiResponse.Ok("Đã cập nhật giá."));
    }
}

public record UpdateProductPriceRequest(decimal Price, decimal? SalePrice);

/// <summary>Danh mục sản phẩm.</summary>
public class CategoriesController : ApiController
{
    public CategoriesController(ISender mediator) : base(mediator) { }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetCategoriesQuery(onlyActive))));
}
