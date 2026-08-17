using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Review.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Review.Presentation;

public class ReviewsController : ApiController
{
    public ReviewsController(ISender mediator) : base(mediator) { }

    /// <summary>Gửi đánh giá sản phẩm (chờ kiểm duyệt).</summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitReviewCommand command)
    {
        var review = await Mediator.Send(command);
        return Ok(ApiResponse<object>.Ok(review, "Cảm ơn bạn! Đánh giá đang chờ kiểm duyệt."));
    }

    /// <summary>Danh sách đánh giá đã duyệt của sản phẩm.</summary>
    [HttpGet("product/{productId:int}")]
    public async Task<IActionResult> GetByProduct(int productId, [FromQuery] int page = 1)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetProductReviewsQuery(productId, page))));
}
