using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Domain;
using Harness.Modules.Promotion.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Promotion.Application;

public record CreateFlashSaleCommand(string Name, DateTimeOffset StartAt, DateTimeOffset EndAt) : IRequest<int>;

public class CreateFlashSaleCommandValidator : AbstractValidator<CreateFlashSaleCommand>
{
    public CreateFlashSaleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt).WithMessage("Thời gian kết thúc phải sau thời gian bắt đầu.");
    }
}

public class CreateFlashSaleCommandHandler : IRequestHandler<CreateFlashSaleCommand, int>
{
    private readonly IHarnessDbContext _db;

    public CreateFlashSaleCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<int> Handle(CreateFlashSaleCommand request, CancellationToken cancellationToken)
    {
        var flashSale = FlashSale.Create(request.Name, request.StartAt, request.EndAt);
        _db.Set<FlashSale>().Add(flashSale);
        await _db.SaveChangesAsync(cancellationToken);
        return flashSale.Id;
    }
}

public record AddFlashSaleItemCommand(int FlashSaleId, int ProductId, decimal SalePrice, int QuantityLimit) : IRequest<int>;

public class AddFlashSaleItemCommandValidator : AbstractValidator<AddFlashSaleItemCommand>
{
    public AddFlashSaleItemCommandValidator()
    {
        RuleFor(x => x.FlashSaleId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SalePrice).GreaterThan(0);
        RuleFor(x => x.QuantityLimit).GreaterThan(0);
    }
}

public class AddFlashSaleItemCommandHandler : IRequestHandler<AddFlashSaleItemCommand, int>
{
    private readonly IHarnessDbContext _db;

    public AddFlashSaleItemCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<int> Handle(AddFlashSaleItemCommand request, CancellationToken cancellationToken)
    {
        var flashSale = await _db.Set<FlashSale>().Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == request.FlashSaleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy flash sale #{request.FlashSaleId}.");

        var item = flashSale.AddItem(request.ProductId, request.SalePrice, request.QuantityLimit);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}

public record GetActiveFlashSalesQuery : IRequest<IReadOnlyList<FlashSaleDto>>;

public class GetActiveFlashSalesQueryHandler : IRequestHandler<GetActiveFlashSalesQuery, IReadOnlyList<FlashSaleDto>>
{
    private readonly IHarnessDbContext _db;

    public GetActiveFlashSalesQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<FlashSaleDto>> Handle(GetActiveFlashSalesQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sales = await _db.Set<FlashSale>().AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.IsActive && x.StartAt <= now && x.EndAt >= now)
            .OrderBy(x => x.StartAt)
            .ToListAsync(cancellationToken);

        var productIds = sales.SelectMany(x => x.Items.Select(i => i.ProductId)).Distinct().ToList();
        var products = await _db.Set<Product>().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);

        return sales.Select(x => Map(x, products)).ToList();
    }

    private static FlashSaleDto Map(FlashSale flashSale, IReadOnlyDictionary<int, Product> products)
    {
        return new FlashSaleDto(
            flashSale.Id,
            flashSale.Name,
            flashSale.StartAt,
            flashSale.EndAt,
            flashSale.Items.Select(i =>
            {
                var hasProduct = products.TryGetValue(i.ProductId, out var product);
                return new FlashSaleItemDto(
                    i.Id, i.ProductId,
                    product?.Name ?? $"Sản phẩm #{i.ProductId}",
                    product?.Slug,
                    product?.Price,
                    product?.ImageUrls.FirstOrDefault(),
                    i.SalePrice, i.QuantityLimit, i.QuantitySold, i.IsSoldOut);
            }).ToList());
    }
}

public record FlashSaleItemDto(
    int Id, int ProductId, string ProductName, string? ProductSlug,
    decimal? ProductPrice, string? ImageUrl,
    decimal SalePrice, int QuantityLimit, int QuantitySold, bool IsSoldOut);

public record FlashSaleDto(int Id, string Name, DateTimeOffset StartAt, DateTimeOffset EndAt, IReadOnlyList<FlashSaleItemDto> Items);
