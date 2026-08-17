using FluentValidation;
using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Catalog.Application;

public record ComboItemInput(int ProductId, int Quantity);

public record CreateComboCommand(
    string Name,
    RoomType RoomType,
    string? Description = null,
    decimal? DiscountedPrice = null,
    List<ComboItemInput>? Items = null) : IRequest<ComboDto>;

public class CreateComboCommandValidator : AbstractValidator<CreateComboCommand>
{
    public CreateComboCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RoomType).IsInEnum();
        RuleFor(x => x.DiscountedPrice).GreaterThan(0).When(x => x.DiscountedPrice.HasValue);
        RuleFor(x => x.Items).Must(i => i is { Count: > 0 }).WithMessage("Combo phải có ít nhất 1 sản phẩm.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.ProductId).GreaterThan(0);
            i.RuleFor(x => x.Quantity).InclusiveBetween(1, 20);
        });
    }
}

public class CreateComboCommandHandler : IRequestHandler<CreateComboCommand, ComboDto>
{
    private readonly IHarnessDbContext _db;

    public CreateComboCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ComboDto> Handle(CreateComboCommand request, CancellationToken cancellationToken)
    {
        var slug = SlugHelper.Generate(request.Name);
        var combo = RoomCombo.Create(request.Name, slug, request.RoomType, request.Description, request.DiscountedPrice);

        foreach (var item in request.Items ?? new List<ComboItemInput>())
            combo.AddItem(item.ProductId, item.Quantity);

        _db.Set<RoomCombo>().Add(combo);
        await _db.SaveChangesAsync(cancellationToken);

        return await ComboMapper.ToDtoAsync(_db, combo, cancellationToken);
    }
}

public record GetCombosQuery(bool OnlyActive = true) : IRequest<IReadOnlyList<ComboDto>>;

public class GetCombosQueryHandler : IRequestHandler<GetCombosQuery, IReadOnlyList<ComboDto>>
{
    private readonly IHarnessDbContext _db;

    public GetCombosQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<ComboDto>> Handle(GetCombosQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<RoomCombo>().AsNoTracking().Include(c => c.Items);
        if (request.OnlyActive) query = query.Where(c => c.IsActive);

        var combos = await query.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        var products = await LoadProductsAsync(combos, cancellationToken);
        return combos.Select(c => ComboMapper.ToDto(c, products)).ToList();
    }

    private async Task<Dictionary<int, Product>> LoadProductsAsync(
        IReadOnlyCollection<RoomCombo> combos, CancellationToken cancellationToken)
    {
        var ids = combos.SelectMany(c => c.Items).Select(i => i.ProductId).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, Product>();
        var products = await _db.Set<Product>().AsNoTracking()
            .Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
        return products.ToDictionary(p => p.Id);
    }
}

public record GetComboBySlugQuery(string Slug) : IRequest<ComboDto?>;

public class GetComboBySlugQueryHandler : IRequestHandler<GetComboBySlugQuery, ComboDto?>
{
    private readonly IHarnessDbContext _db;

    public GetComboBySlugQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<ComboDto?> Handle(GetComboBySlugQuery request, CancellationToken cancellationToken)
    {
        var combo = await _db.Set<RoomCombo>().AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Slug == request.Slug && c.IsActive, cancellationToken);
        if (combo is null) return null;

        var products = await _db.Set<Product>().AsNoTracking()
            .Where(p => combo.Items.Select(i => i.ProductId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return ComboMapper.ToDto(combo, products);
    }
}

public record ComboItemDto(int ProductId, string? ProductName, string? ProductSlug, int Quantity, decimal UnitPrice, decimal Subtotal);

public record ComboDto(
    int Id, string Name, string Slug, string RoomType, string RoomTypeLabel, string? Description,
    bool IsActive, decimal? DiscountedPrice, decimal RegularTotal, decimal SaleTotal, decimal Savings,
    IReadOnlyList<ComboItemDto> Items);

internal static class ComboMapper
{
    public static async Task<ComboDto> ToDtoAsync(IHarnessDbContext db, RoomCombo combo, CancellationToken cancellationToken)
    {
        var ids = combo.Items.Select(i => i.ProductId).Distinct().ToList();
        Dictionary<int, Product> products = new();
        if (ids.Count > 0)
        {
            products = await db.Set<Product>().AsNoTracking()
                .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        }
        return ToDto(combo, products);
    }

    public static ComboDto ToDto(RoomCombo combo, IReadOnlyDictionary<int, Product> products)
    {
        var items = combo.Items
            .OrderBy(i => i.SortOrder)
            .Select(i =>
            {
                products.TryGetValue(i.ProductId, out var p);
                var unitPrice = p?.Price ?? 0;
                return new ComboItemDto(i.ProductId, p?.Name, p?.Slug, i.Quantity, unitPrice, unitPrice * i.Quantity);
            })
            .ToList();

        var regularTotal = items.Sum(i => i.Subtotal);
        var saleTotal = combo.DiscountedPrice ?? regularTotal;
        var savings = Math.Max(0, regularTotal - saleTotal);
        var label = combo.RoomType switch
        {
            RoomType.LivingRoom => "Phòng khách",
            RoomType.BedRoom => "Phòng ngủ",
            RoomType.DiningRoom => "Phòng bếp/ăn",
            RoomType.HomeOffice => "Văn phòng tại nhà",
            _ => combo.RoomType.ToString()
        };

        return new ComboDto(
            combo.Id, combo.Name, combo.Slug, combo.RoomType.ToString(), label, combo.Description,
            combo.IsActive, combo.DiscountedPrice, regularTotal, saleTotal, savings, items);
    }
}
