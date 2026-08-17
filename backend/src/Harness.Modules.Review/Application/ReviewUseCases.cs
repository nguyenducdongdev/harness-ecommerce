using FluentValidation;
using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Review.Domain;
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
        var review = Review.Submit(request.ProductId, request.CustomerName, request.CustomerPhone,
            request.Rating, request.Content, request.ImageUrls);

        _db.Set<Review>().Add(review);
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
        var query = _db.Set<Review>().AsNoTracking()
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
