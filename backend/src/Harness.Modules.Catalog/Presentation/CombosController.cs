using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Catalog.Application;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Catalog.Presentation;

/// <summary>API combo phòng — gộp sản phẩm thành không gian hoàn chỉnh (sofa + bàn + kệ...).</summary>
public class CombosController : ApiController
{
    public CombosController(ISender mediator) : base(mediator) { }

    /// <summary>Danh sách combo phòng (tính giá động từ sản phẩm thành phần).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ComboDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = true)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetCombosQuery(onlyActive))));

    /// <summary>Chi tiết combo theo slug.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var combo = await Mediator.Send(new GetComboBySlugQuery(slug));
        return combo is null
            ? NotFound(ApiResponse.Fail($"Không tìm thấy combo '{slug}'."))
            : Ok(ApiResponse.Ok(combo));
    }

    /// <summary>Tạo combo mới (admin).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateComboCommand command)
    {
        var combo = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetBySlug), new { slug = combo.Slug }, ApiResponse<object>.Ok(combo, "Đã tạo combo."));
    }
}
