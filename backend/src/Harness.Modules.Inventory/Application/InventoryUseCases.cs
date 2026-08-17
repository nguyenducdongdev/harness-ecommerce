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

public record SetStockCommand(int WarehouseId, string VariantSku, int Quantity, string Reference) : IRequest<StockDto>;

public class SetStockCommandValidator : AbstractValidator<SetStockCommand>
{
    public SetStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
    }
}

public class SetStockCommandHandler : IRequestHandler<SetStockCommand, StockDto>
{
    private readonly IHarnessDbContext _db;

    public SetStockCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<StockDto> Handle(SetStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _db.Set<StockLevel>()
            .FirstOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.VariantSku == request.VariantSku, cancellationToken);

        int delta;
        if (stock is null)
        {
            stock = StockLevel.Create(request.WarehouseId, request.VariantSku, request.Quantity);
            _db.Set<StockLevel>().Add(stock);
            delta = request.Quantity;
        }
        else
        {
            delta = request.Quantity - stock.QuantityAvailable;
            stock.Adjust(delta);
        }

        _db.Set<StockMovement>().Add(new StockMovement
        {
            Id = Guid.NewGuid(), WarehouseId = request.WarehouseId, VariantSku = request.VariantSku,
            Type = MovementType.Adjustment, Quantity = delta, Reference = request.Reference
        });
        _db.AddToOutbox(new StockChangedIntegrationEvent(request.WarehouseId, request.VariantSku, stock.QuantityAvailable, request.Reference));
        await _db.SaveChangesAsync(cancellationToken);

        return new StockDto(stock.WarehouseId, stock.VariantSku, stock.QuantityAvailable, stock.QuantityReserved);
    }
}

public record ReserveStockCommand(int WarehouseId, string VariantSku, int Quantity, string Reference) : IRequest<StockDto>;

public class ReserveStockCommandValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(50);
    }
}

public class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, StockDto>
{
    private readonly IHarnessDbContext _db;

    public ReserveStockCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<StockDto> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _db.Set<StockLevel>()
            .FirstOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.VariantSku == request.VariantSku, cancellationToken)
            ?? throw new InvalidOperationException($"Chưa khai báo tồn kho cho SKU {request.VariantSku} ở kho #{request.WarehouseId}.");

        stock.Reserve(request.Quantity);

        _db.Set<StockMovement>().Add(new StockMovement
        {
            Id = Guid.NewGuid(), WarehouseId = request.WarehouseId, VariantSku = request.VariantSku,
            Type = MovementType.Reservation, Quantity = request.Quantity, Reference = request.Reference
        });
        _db.AddToOutbox(new StockChangedIntegrationEvent(request.WarehouseId, request.VariantSku, stock.QuantityAvailable, request.Reference));
        await _db.SaveChangesAsync(cancellationToken);

        return new StockDto(stock.WarehouseId, stock.VariantSku, stock.QuantityAvailable, stock.QuantityReserved);
    }
}

public record ReleaseStockCommand(int WarehouseId, string VariantSku, int Quantity, string Reference) : IRequest<StockDto>;

public class ReleaseStockCommandValidator : AbstractValidator<ReleaseStockCommand>
{
    public ReleaseStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(50);
    }
}

public class ReleaseStockCommandHandler : IRequestHandler<ReleaseStockCommand, StockDto>
{
    private readonly IHarnessDbContext _db;

    public ReleaseStockCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<StockDto> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await _db.Set<StockLevel>()
            .FirstOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.VariantSku == request.VariantSku, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy tồn kho SKU {request.VariantSku} ở kho #{request.WarehouseId}.");

        stock.ReleaseReservation(request.Quantity);

        _db.Set<StockMovement>().Add(new StockMovement
        {
            Id = Guid.NewGuid(), WarehouseId = request.WarehouseId, VariantSku = request.VariantSku,
            Type = MovementType.Release, Quantity = request.Quantity, Reference = request.Reference
        });
        _db.AddToOutbox(new StockChangedIntegrationEvent(request.WarehouseId, request.VariantSku, stock.QuantityAvailable, request.Reference));
        await _db.SaveChangesAsync(cancellationToken);

        return new StockDto(stock.WarehouseId, stock.VariantSku, stock.QuantityAvailable, stock.QuantityReserved);
    }
}

public record TransferStockCommand(
    int FromWarehouseId, int ToWarehouseId, string VariantSku, int Quantity, string Reference) : IRequest<IReadOnlyList<StockDto>>;

public class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockCommandValidator()
    {
        RuleFor(x => x.FromWarehouseId).GreaterThan(0);
        RuleFor(x => x.ToWarehouseId).GreaterThan(0);
        RuleFor(x => x.FromWarehouseId).NotEqual(x => x.ToWarehouseId)
            .WithMessage("Kho nguồn và kho đích phải khác nhau.");
        RuleFor(x => x.VariantSku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(50);
    }
}

public class TransferStockCommandHandler : IRequestHandler<TransferStockCommand, IReadOnlyList<StockDto>>
{
    private readonly IHarnessDbContext _db;

    public TransferStockCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<IReadOnlyList<StockDto>> Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        var from = await _db.Set<StockLevel>()
            .FirstOrDefaultAsync(s => s.WarehouseId == request.FromWarehouseId && s.VariantSku == request.VariantSku, cancellationToken)
            ?? throw new InvalidOperationException($"Kho nguồn #{request.FromWarehouseId} chưa khai báo tồn kho {request.VariantSku}.");

        from.Adjust(-request.Quantity); // ngăn chuyển nhiều hơn tồn kho

        var to = await _db.Set<StockLevel>()
            .FirstOrDefaultAsync(s => s.WarehouseId == request.ToWarehouseId && s.VariantSku == request.VariantSku, cancellationToken);
        if (to is null)
        {
            to = StockLevel.Create(request.ToWarehouseId, request.VariantSku, request.Quantity);
            _db.Set<StockLevel>().Add(to);
        }
        else
        {
            to.Adjust(request.Quantity);
        }

        _db.Set<StockMovement>().Add(new StockMovement
        {
            Id = Guid.NewGuid(), WarehouseId = request.FromWarehouseId, VariantSku = request.VariantSku,
            Type = MovementType.TransferOut, Quantity = request.Quantity, Reference = request.Reference
        });
        _db.Set<StockMovement>().Add(new StockMovement
        {
            Id = Guid.NewGuid(), WarehouseId = request.ToWarehouseId, VariantSku = request.VariantSku,
            Type = MovementType.TransferIn, Quantity = request.Quantity, Reference = request.Reference
        });
        _db.AddToOutbox(new StockChangedIntegrationEvent(request.FromWarehouseId, request.VariantSku, from.QuantityAvailable, request.Reference));
        _db.AddToOutbox(new StockChangedIntegrationEvent(request.ToWarehouseId, request.VariantSku, to.QuantityAvailable, request.Reference));
        await _db.SaveChangesAsync(cancellationToken);

        return new[]
        {
            new StockDto(from.WarehouseId, from.VariantSku, from.QuantityAvailable, from.QuantityReserved),
            new StockDto(to.WarehouseId, to.VariantSku, to.QuantityAvailable, to.QuantityReserved)
        };
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
