using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Inventory.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Inventory.Application;

public record AdjustStockCommand(int WarehouseId, string VariantSku, int Delta, string Reference) : IRequest<StockDto>;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Số lượng điều chỉnh phải khác 0.");
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(50);
    }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, StockDto>
{
    private readonly IHarnessDbContext _db;

    public AdjustStockCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<StockDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _db.Set<StockLevel>()
            .FirstOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.VariantSku == request.VariantSku, cancellationToken);

        if (stock is null)
        {
            if (request.Delta < 0)
                throw new InvalidOperationException("Không thể trừ tồn kho chưa khởi tạo.");
            stock = StockLevel.Create(request.WarehouseId, request.VariantSku, request.Delta);
            _db.Set<StockLevel>().Add(stock);
        }
        else
        {
            stock.Adjust(request.Delta);
        }

        _db.Set<StockMovement>().Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            WarehouseId = request.WarehouseId,
            VariantSku = request.VariantSku,
            Type = MovementType.Adjustment,
            Quantity = request.Delta,
            Reference = request.Reference
        });
        _db.AddToOutbox(new StockChangedIntegrationEvent(
            request.WarehouseId, request.VariantSku, stock.QuantityAvailable, request.Reference));

        await _db.SaveChangesAsync(cancellationToken);
        return new StockDto(stock.WarehouseId, stock.VariantSku, stock.QuantityAvailable, stock.QuantityReserved);
    }
}

public record StockDto(int WarehouseId, string VariantSku, int QuantityAvailable, int QuantityReserved);

public record GetStockQuery(string VariantSku) : IRequest<IReadOnlyList<StockDto>>;

public class GetStockQueryHandler : IRequestHandler<GetStockQuery, IReadOnlyList<StockDto>>
{
    private readonly IHarnessDbContext _db;

    public GetStockQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockDto>> Handle(GetStockQuery request, CancellationToken cancellationToken)
        => await _db.Set<StockLevel>().AsNoTracking()
            .Where(s => s.VariantSku == request.VariantSku)
            .Select(s => new StockDto(s.WarehouseId, s.VariantSku, s.QuantityAvailable, s.QuantityReserved))
            .ToListAsync(cancellationToken);
}

public record GetWarehousesQuery(bool OnlyActive = true) : IRequest<IReadOnlyList<WarehouseDto>>;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    private readonly IHarnessDbContext _db;

    public GetWarehousesQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<Warehouse>().AsNoTracking();
        if (request.OnlyActive) query = query.Where(w => w.IsActive);
        return await query.OrderBy(w => w.Id)
            .Select(w => new WarehouseDto(w.Id, w.Code, w.Name, w.Address, w.IsShowroom))
            .ToListAsync(cancellationToken);
    }
}

public record WarehouseDto(int Id, string Code, string Name, string Address, bool IsShowroom);
