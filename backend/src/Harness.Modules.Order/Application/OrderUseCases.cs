using FluentValidation;
using Harness.BuildingBlocks.Application.Common;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Order.Domain;
using OrderEntity = Harness.Modules.Order.Domain.Order;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Order.Application;

public record OrderItemInput(int ProductId, string VariantSku, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderCommand(
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string ShippingAddress,
    string? Note,
    DeliveryMethod DeliveryMethod,
    PaymentMethod PaymentMethod,
    List<OrderItemInput> Items,
    decimal ShippingFee = 0,
    decimal DiscountAmount = 0,
    int? WarehouseId = null) : IRequest<OrderDto>;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).NotEmpty().Matches(@"^0\d{9,10}$")
            .WithMessage("Số điện thoại VN không hợp lệ (VD: 0912345678).");
        RuleFor(x => x.CustomerEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.CustomerEmail));
        RuleFor(x => x.ShippingAddress).NotEmpty().MaximumLength(500)
            .When(x => x.DeliveryMethod != DeliveryMethod.PickupAtStore);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Đơn hàng phải có ít nhất 1 sản phẩm.");
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.Quantity).InclusiveBetween(1, 50);
            i.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            i.RuleFor(x => x.VariantSku).NotEmpty();
        });
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IHarnessDbContext _db;

    public CreateOrderCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = OrderEntity.Create(
            request.CustomerName, request.CustomerPhone, request.CustomerEmail,
            request.ShippingAddress, request.Note,
            request.DeliveryMethod, request.PaymentMethod,
            request.Items.Select(i => (i.ProductId, i.VariantSku, i.ProductName, i.UnitPrice, i.Quantity)),
            request.ShippingFee, request.DiscountAmount, request.WarehouseId);

        _db.Set<OrderEntity>().Add(order);
        // Outbox: ERP tạo bút toán, DMS giữ chỗ tồn kho sau khi đơn được ghi nhận
        _db.AddToOutbox(new OrderCreatedIntegrationEvent(
            order.Id, order.OrderNumber, order.TotalAmount,
            order.CustomerPhone, order.DeliveryMethod.ToString(), order.PaymentMethod.ToString()));
        await _db.SaveChangesAsync(cancellationToken);

        return OrderMapper.ToDto(order);
    }
}

public record OrderDto(
    Guid Id, string OrderNumber, OrderStatus Status,
    string CustomerName, string CustomerPhone,
    decimal ItemsTotal, decimal ShippingFee, decimal DiscountAmount, decimal TotalAmount,
    DeliveryMethod DeliveryMethod, PaymentMethod PaymentMethod,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(int ProductId, string VariantSku, string ProductName, decimal UnitPrice, int Quantity);

public record GetOrderQuery(string OrderNumber) : IRequest<OrderDto?>;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    private readonly IHarnessDbContext _db;

    public GetOrderQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Set<OrderEntity>().AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == request.OrderNumber, cancellationToken);
        return order is null ? null : OrderMapper.ToDto(order);
    }
}

public record GetOrdersByPhoneQuery(string Phone, int Page = 1, int PageSize = 10) : IRequest<PagedResult<OrderDto>>;

public class GetOrdersByPhoneQueryHandler : IRequestHandler<GetOrdersByPhoneQuery, PagedResult<OrderDto>>
{
    private readonly IHarnessDbContext _db;

    public GetOrdersByPhoneQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<PagedResult<OrderDto>> Handle(GetOrdersByPhoneQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var query = _db.Set<OrderEntity>().AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerPhone == request.Phone)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.LongCountAsync(cancellationToken);
        var orders = await query.Skip((page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);

        return PagedResult<OrderDto>.Create(
            orders.Select(OrderMapper.ToDto).ToList(), total, page, request.PageSize);
    }
}

internal static class OrderMapper
{
    public static OrderDto ToDto(OrderEntity o) => new(
        o.Id, o.OrderNumber, o.Status, o.CustomerName, o.CustomerPhone,
        o.ItemsTotal, o.ShippingFee, o.DiscountAmount, o.TotalAmount,
        o.DeliveryMethod, o.PaymentMethod,
        o.Items.Select(i => new OrderItemDto(i.ProductId, i.VariantSku, i.ProductName, i.UnitPrice, i.Quantity)).ToList());
}
