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

    /// <summary>Tổng hợp điểm đánh giá sản phẩm.</summary>
    [HttpGet("product/{productId:int}/rating")]
    public async Task<IActionResult> GetRating(int productId)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetProductRatingQuery(productId))));

    // ===== Kiểm duyệt (admin) =====

    /// <summary>Hàng chờ đánh giá chưa duyệt.</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] int page = 1)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetReviewModerationQueueQuery(page))));

    /// <summary>Duyệt đánh giá.</summary>
    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
        => Ok(ApiResponse<object>.Ok(await Mediator.Send(new ApproveReviewCommand(id)), "Đã duyệt đánh giá."));

    /// <summary>Từ chối đánh giá.</summary>
    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id)
        => Ok(ApiResponse<object>.Ok(await Mediator.Send(new RejectReviewCommand(id)), "Đã từ chối đánh giá."));
}
