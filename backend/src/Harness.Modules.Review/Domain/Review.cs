using Harness.BuildingBlocks.Domain;

namespace Harness.Modules.Review.Domain;

public enum ReviewStatus { Pending = 1, Approved = 2, Rejected = 3 }

/// <summary>Đánh giá sản phẩm — có kiểm duyệt, có xác minh đã mua.</summary>
public class Review : AuditableEntity<Guid>
{
    public int ProductId { get; private set; }
    public string CustomerName { get; private set; } = default!;
    public string CustomerPhone { get; private set; } = default!; // để xác minh mua hàng (không hiển thị)
    public int Rating { get; private set; } // 1-5 sao
    public string Content { get; private set; } = default!;
    public List<string> ImageUrls { get; private set; } = new(); // jsonb
    public ReviewStatus Status { get; private set; } = ReviewStatus.Pending;
    public bool VerifiedPurchase { get; private set; }

    private Review() { } // EF

    public static Review Submit(int productId, string customerName, string customerPhone,
        int rating, string content, List<string>? imageUrls = null, bool verifiedPurchase = false)
    {
        if (rating is < 1 or > 5) throw new ArgumentException("Số sao phải từ 1 đến 5.");
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Nội dung đánh giá không được trống.");

        return new Review
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            Rating = rating,
            Content = content,
            ImageUrls = imageUrls ?? new List<string>(),
            VerifiedPurchase = verifiedPurchase
        };
    }

    public void Approve() => Status = ReviewStatus.Approved;
    public void Reject() => Status = ReviewStatus.Rejected;
}
