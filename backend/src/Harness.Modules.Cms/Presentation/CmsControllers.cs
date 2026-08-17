using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Cms.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Cms.Presentation;

public class BannersController : ApiController
{
    public BannersController(ISender mediator) : base(mediator) { }

    /// <summary>Banner đang chạy theo vị trí.</summary>
    [HttpGet]
    public async Task<IActionResult> GetActive([FromQuery] string position = "home-hero")
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetActiveBannersQuery(position))));
}

public class PagesController : ApiController
{
    public PagesController(ISender mediator) : base(mediator) { }

    /// <summary>Trang nội dung theo slug.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var page = await Mediator.Send(new GetPageBySlugQuery(slug));
        return page is null
            ? NotFound(ApiResponse.Fail("Không tìm thấy trang."))
            : Ok(ApiResponse.Ok(page));
    }
}
