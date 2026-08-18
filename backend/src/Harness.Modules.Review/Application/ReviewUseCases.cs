using FluentValidation;
using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Review.Domain;
using ReviewEntity = Harness.Modules.Review.Domain.Review;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Review.Application;

public record SubmitReviewCommand(
    int ProductId, string CustomerName, string CustomerPhone,
    int Rating, string Content, List<string>? ImageUrls = null) : IRequest<ReviewDto>;

public class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerPhone).Matches(@"^0\d{9,10}$");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ImageUrls).Must(urls => urls is null || urls.Count <= 6)
            .WithMessage("Tối đa 6 ảnh cho đánh giá.");
    }
}

public class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, ReviewDto>
{
    private readonly IHarnessDbContext _db;

    public SubmitReviewCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ReviewDto> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        // TODO Phase 2: đối chiếu CustomerPhone với đơn đã giao của sản phẩm này → VerifiedPurchase
        var review = ReviewEntity.Submit(request.ProductId, request.CustomerName, request.CustomerPhone,
            request.Rating, request.Content, request.ImageUrls);

        _db.Set<ReviewEntity>().Add(review);
        await _db.SaveChangesAsync(cancellationToken);
        return new ReviewDto(review.Id, review.ProductId, review.CustomerName,
            review.Rating, review.Content, review.VerifiedPurchase, review.Status.ToString());
    }
}

public record ReviewDto(Guid Id, int ProductId, string CustomerName, int Rating,
    string Content, bool VerifiedPurchase, string Status);

public record GetProductReviewsQuery(int ProductId, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<ReviewDto>>;

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, PagedResult<ReviewDto>>
{
    private readonly IHarnessDbContext _db;

    public GetProductReviewsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PagedResult<ReviewDto>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var query = _db.Set<ReviewEntity>().AsNoTracking()
            .Where(r => r.ProductId == request.ProductId && r.Status == ReviewStatus.Approved);

        var total = await query.LongCountAsync(cancellationToken);
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * request.PageSize).Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<ReviewDto>.Create(
            reviews.Select(r => new ReviewDto(r.Id, r.ProductId, r.CustomerName,
                r.Rating, r.Content, r.VerifiedPurchase, r.Status.ToString())).ToList(),
            total, page, request.PageSize);
    }
}
public record ApproveReviewCommand(Guid Id) : IRequest<ReviewDto>;

public class ApproveReviewCommandHandler : IRequestHandler<ApproveReviewCommand, ReviewDto>
{
    private readonly IHarnessDbContext _db;
    public ApproveReviewCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ReviewDto> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _db.Set<ReviewEntity>().FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đánh giá.");
        review.Approve();
        await _db.SaveChangesAsync(cancellationToken);
        return ReviewMapper.ToDto(review);
    }
}

public record RejectReviewCommand(Guid Id) : IRequest<ReviewDto>;

public class RejectReviewCommandHandler : IRequestHandler<RejectReviewCommand, ReviewDto>
{
    private readonly IHarnessDbContext _db;
    public RejectReviewCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ReviewDto> Handle(RejectReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _db.Set<ReviewEntity>().FindAsync(new object[] { request.Id }, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đánh giá.");
        review.Reject();
        await _db.SaveChangesAsync(cancellationToken);
        return ReviewMapper.ToDto(review);
    }
}

/// <summary>Hàng chờ kiểm duyệt (admin).</summary>
public record GetReviewModerationQueueQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ReviewQueueItemDto>>;

public class GetReviewModerationQueueQueryHandler : IRequestHandler<GetReviewModerationQueueQuery, PagedResult<ReviewQueueItemDto>>
{
    private readonly IHarnessDbContext _db;
    public GetReviewModerationQueueQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PagedResult<ReviewQueueItemDto>> Handle(GetReviewModerationQueueQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var query = _db.Set<ReviewEntity>().AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Pending)
            .OrderBy(r => r.CreatedAt);

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return PagedResult<ReviewQueueItemDto>.Create(
            items.Select(r => new ReviewQueueItemDto(
                r.Id, r.ProductId, r.CustomerName, r.Rating, r.Content, r.VerifiedPurchase, r.CreatedAt)).ToList(),
            total, page, request.PageSize);
    }
}

public record ReviewQueueItemDto(Guid Id, int ProductId, string CustomerName, int Rating, string Content, bool VerifiedPurchase, DateTimeOffset CreatedAt);

/// <summary>Tổng hợp điểm đánh giá sản phẩm (trung bình + phân bố sao).</summary>
public record GetProductRatingQuery(int ProductId) : IRequest<ProductRatingDto>;

public class GetProductRatingQueryHandler : IRequestHandler<GetProductRatingQuery, ProductRatingDto>
{
    private readonly IHarnessDbContext _db;
    public GetProductRatingQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<ProductRatingDto> Handle(GetProductRatingQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _db.Set<ReviewEntity>().AsNoTracking()
            .Where(r => r.ProductId == request.ProductId && r.Status == ReviewStatus.Approved)
            .ToListAsync(cancellationToken);

        var total = reviews.Count;
        var average = total == 0 ? 0 : (decimal)Math.Round(reviews.Average(r => r.Rating), 1);
        return new ProductRatingDto(
            request.ProductId, average, total,
            Enumerable.Range(1, 5).Select(star => new RatingBucketsDto(star, reviews.Count(r => r.Rating == star))).ToList());
    }
}

public record RatingBucketsDto(int Star, int Count);
public record ProductRatingDto(int ProductId, decimal AverageRating, int TotalCount, List<RatingBucketsDto> Ratings);

internal static class ReviewMapper
{
    public static ReviewDto ToDto(ReviewEntity r) => new(
        r.Id, r.ProductId, r.CustomerName, r.Rating, r.Content, r.VerifiedPurchase, r.Status.ToString());
}
