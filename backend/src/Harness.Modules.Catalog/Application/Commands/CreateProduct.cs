using FluentValidation;
using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Catalog.Application.Dtos;
using Harness.Modules.Catalog.Domain;
using MediatR;

namespace Harness.Modules.Catalog.Application.Commands;

public record CreateProductVariantInput(
    string Sku,
    string SizeName,
    int WidthCm,
    int DepthCm,
    int HeightCm,
    string? Color = null,
    decimal? PriceOverride = null);

public record CreateProductCommand(
    string Name,
    int CategoryId,
    int BrandId,
    decimal Price,
    decimal? SalePrice,
    int WarrantyMonths,
    string? ShortDescription = null,
    string? Description = null,
    Dictionary<string, string>? Attributes = null,
    List<string>? ImageUrls = null,
    bool IsFeatured = false,
    List<CreateProductVariantInput>? Variants = null) : IRequest<ProductDto>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300)
            .WithMessage("Tên sản phẩm không được trống và tối đa 300 ký tự.");
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.BrandId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Giá bán phải lớn hơn 0.");
        RuleFor(x => x.SalePrice).LessThan(x => x.Price)
            .When(x => x.SalePrice.HasValue)
            .WithMessage("Giá khuyến mãi phải nhỏ hơn giá gốc.");
        RuleFor(x => x.WarrantyMonths).InclusiveBetween(0, 360);
        RuleForEach(x => x.Variants).ChildRules(v =>
        {
            v.RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
            v.RuleFor(x => x.WidthCm).InclusiveBetween(20, 400);
            v.RuleFor(x => x.DepthCm).InclusiveBetween(10, 400);
            v.RuleFor(x => x.HeightCm).InclusiveBetween(10, 250);
        });
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IHarnessDbContext _db;

    public CreateProductCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var slug = SlugHelper.Generate(request.Name);
        var sku = $"NT-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var product = Product.Create(
            request.Name, slug, sku, request.CategoryId, request.BrandId,
            request.Price, request.SalePrice, request.WarrantyMonths,
            request.ShortDescription, request.Description,
            request.Attributes, request.ImageUrls, request.IsFeatured);

        if (request.Variants is { Count: > 0 })
        {
            foreach (var v in request.Variants)
                product.AddVariant(ProductVariant.Create(
                    product.Id, v.Sku, v.SizeName, v.WidthCm, v.DepthCm, v.HeightCm, v.Color, v.PriceOverride));
        }

        // Outbox: sau khi SaveChanges thành công, event sẽ được publish lên RabbitMQ bởi OutboxPublisherJob
        _db.Set<Product>().Add(product);
        _db.AddToOutbox(new ProductCreatedIntegrationEvent(0, slug, request.Name));
        await _db.SaveChangesAsync(cancellationToken);

        var category = await _db.Set<Category>().FindAsync(new object[] { request.CategoryId }, cancellationToken);
        var brand = await _db.Set<Brand>().FindAsync(new object[] { request.BrandId }, cancellationToken);

        return ProductMapper.ToDto(product, category?.Name, brand?.Name, category?.Slug);
    }
}

public record UpdateProductPriceCommand(int ProductId, decimal Price, decimal? SalePrice) : IRequest<Unit>;

public class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.SalePrice).LessThan(x => x.Price).When(x => x.SalePrice.HasValue);
    }
}

public class UpdateProductPriceCommandHandler : IRequestHandler<UpdateProductPriceCommand, Unit>
{
    private readonly IHarnessDbContext _db;

    public UpdateProductPriceCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Set<Product>().FindAsync(new object[] { request.ProductId }, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy sản phẩm #{request.ProductId}.");

        product.UpdatePrice(request.Price, request.SalePrice);
        _db.AddToOutbox(new ProductUpdatedIntegrationEvent(product.Id, request.Price, request.SalePrice));
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

/// <summary>Mapper entity → DTO (thủ công, tránh phụ thuộc AutoMapper).</summary>
internal static class ProductMapper
{
    public static ProductDto ToDto(Product p, string? categoryName = null, string? brandName = null, string? categorySlug = null) => new(
        p.Id, p.Name, p.Slug, p.Sku, p.ShortDescription, p.Description,
        p.CategoryId, categoryName, categorySlug, p.BrandId, brandName,
        p.Price, p.SalePrice, p.WarrantyMonths, p.IsActive, p.IsFeatured, p.ViewCount,
        p.Attributes, p.ImageUrls,
        p.Variants.Select(v => new ProductVariantDto(
            v.Id, v.Sku, v.SizeName, v.WidthCm, v.DepthCm, v.HeightCm, v.Color, v.PriceOverride)).ToList());
}
