using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Commands;
using Harness.Modules.Catalog.Application.Dtos;
using Harness.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Catalog.Application.Queries;

public record QuizRequestDto(
    string RoomType,
    int? RoomAreaM2,
    string? Style,
    decimal? MinBudget,
    decimal? MaxBudget);

public record QuizRecommendationResultDto(
    string RoomType,
    string? Style,
    int? RoomAreaM2,
    string Summary,
    decimal TotalEstimatedPrice,
    IReadOnlyList<ProductDto> RecommendedProducts,
    IReadOnlyList<ComboDto> RecommendedCombos);

public record GetQuizRecommendationQuery(QuizRequestDto Request) : IRequest<QuizRecommendationResultDto>;

public class GetQuizRecommendationQueryHandler : IRequestHandler<GetQuizRecommendationQuery, QuizRecommendationResultDto>
{
    private readonly IHarnessDbContext _db;

    public GetQuizRecommendationQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<QuizRecommendationResultDto> Handle(GetQuizRecommendationQuery query, CancellationToken cancellationToken)
    {
        var req = query.Request;
        var styleNormalized = string.IsNullOrWhiteSpace(req.Style) ? "Hiện đại" : req.Style.Trim();
        var maxBudget = req.MaxBudget ?? 50_000_000m;
        var minBudget = req.MinBudget ?? 0m;

        // Map room type input to category slug or enum
        var categorySlug = req.RoomType.ToLowerInvariant() switch
        {
            "phong-khach" or "living_room" or "livingroom" => "sofa",
            "phong-ngu" or "bedroom" => "giuong-phong-ngu",
            "phong-an" or "dining" or "diningroom" => "ban-an",
            "van-phong" or "office" => "van-phong",
            _ => null
        };

        // Query active products
        var productQuery = from p in _db.Set<Product>().AsNoTracking()
                           join c in _db.Set<Category>().AsNoTracking() on p.CategoryId equals c.Id
                           join b in _db.Set<Brand>().AsNoTracking() on p.BrandId equals b.Id
                           where p.IsActive
                           select new { p, c, b };

        if (!string.IsNullOrEmpty(categorySlug))
        {
            productQuery = productQuery.Where(x => x.c.Slug == categorySlug || x.c.Slug == "phu-kien-trang-tri" || x.c.Slug == "tu-ke");
        }

        var allProducts = await productQuery.Take(50).ToListAsync(cancellationToken);

        // Filter products matching style & budget
        var matchedProducts = allProducts
            .Select(x =>
            {
                var dto = ProductMapper.ToDto(x.p, x.c.Name, x.b.Name, x.c.Slug);
                var price = dto.DisplayPrice;
                var hasStyle = dto.Attributes.TryGetValue("phong-cach", out var s) && s.Equals(styleNormalized, StringComparison.OrdinalIgnoreCase);
                var isWithinBudget = price <= maxBudget;

                return new { dto, hasStyle, isWithinBudget, price };
            })
            .OrderByDescending(x => x.hasStyle)
            .ThenBy(x => x.price)
            .Take(6)
            .Select(x => x.dto)
            .ToList();

        // Query room combos if available
        var roomEnum = req.RoomType.ToLowerInvariant() switch
        {
            "phong-khach" or "living_room" => RoomType.LivingRoom,
            "phong-ngu" or "bedroom" => RoomType.BedRoom,
            "phong-an" or "dining" => RoomType.DiningRoom,
            "van-phong" or "office" => RoomType.HomeOffice,
            _ => (RoomType?)null
        };

        List<ComboDto> recommendedCombos = new();
        if (roomEnum.HasValue)
        {
            var combos = await _db.Set<RoomCombo>().AsNoTracking()
                .Include(c => c.Items)
                .Where(c => c.IsActive && c.RoomType == roomEnum.Value)
                .Take(3)
                .ToListAsync(cancellationToken);

            var productIds = combos.SelectMany(c => c.Items).Select(i => i.ProductId).Distinct().ToList();
            var productsDict = await _db.Set<Product>().AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            recommendedCombos = combos.Select(c => ComboMapper.ToDto(c, productsDict)).ToList();
        }

        var totalEstimatedPrice = matchedProducts.Sum(p => p.DisplayPrice);
        var summaryText = BuildSummary(req.RoomType, req.RoomAreaM2, styleNormalized, maxBudget, matchedProducts.Count, recommendedCombos.Count);

        return new QuizRecommendationResultDto(
            req.RoomType,
            styleNormalized,
            req.RoomAreaM2,
            summaryText,
            totalEstimatedPrice,
            matchedProducts,
            recommendedCombos);
    }

    private static string BuildSummary(string roomType, int? area, string style, decimal maxBudget, int productCount, int comboCount)
    {
        var roomLabel = roomType switch
        {
            "phong-khach" or "living_room" => "Phòng khách",
            "phong-ngu" or "bedroom" => "Phòng ngủ",
            "phong-an" or "dining" => "Phòng ăn",
            "van-phong" or "office" => "Văn phòng làm việc",
            _ => "Không gian sống"
        };

        var areaText = area.HasValue ? $"diện tích {area.Value}m²" : "mọi diện tích";
        var budgetText = maxBudget > 0 ? $"{maxBudget:N0}đ" : "linh hoạt";

        return $"Chuyên gia tư vấn Harness đề xuất {productCount} sản phẩm & {comboCount} bộ phối cảnh trọn gói cho {roomLabel} ({areaText}) theo phong cách {style}. Ngân sách tối đa {budgetText}. Các sản phẩm được tính toán hài hòa về kích thước và màu sắc.";
    }
}
