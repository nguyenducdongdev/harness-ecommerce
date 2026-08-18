using Harness.Modules.Review.Domain;
using ReviewEntity = Harness.Modules.Review.Domain.Review;
using Xunit;

namespace Harness.UnitTests;

/// <summary>Kiểm thử domain đánh giá sản phẩm: nhập liệu + kiểm duyệt.</summary>
public class ReviewTests
{
    [Fact]
    public void Submit_ValidInput_SetsPending()
    {
        var review = ReviewEntity.Submit(1, "Nguyễn Văn A", "0912345678", 5, "Sản phẩm tốt, giao nhanh.");

        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.False(review.VerifiedPurchase);
    }

    [Fact]
    public void Submit_RatingOutOfRange_Throws()
        => Assert.Throws<ArgumentException>(() => ReviewEntity.Submit(1, "A", "0912345678", 0, "nội dung"));

    [Fact]
    public void Submit_EmptyContent_Throws()
        => Assert.Throws<ArgumentException>(() => ReviewEntity.Submit(1, "A", "0912345678", 4, " "));

    [Fact]
    public void Approve_SetsApproved()
    {
        var review = ReviewEntity.Submit(1, "A", "0912345678", 4, "OK");
        review.Approve();
        Assert.Equal(ReviewStatus.Approved, review.Status);
    }

    [Fact]
    public void Reject_SetsRejected()
    {
        var review = ReviewEntity.Submit(1, "A", "0912345678", 4, "OK");
        review.Reject();
        Assert.Equal(ReviewStatus.Rejected, review.Status);
    }
}
