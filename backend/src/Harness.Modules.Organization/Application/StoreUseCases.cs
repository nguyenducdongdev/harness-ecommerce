using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Organization.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Organization.Application;

public record StoreDto(
    Guid Id,
    string Code,
    string Name,
    string Address,
    string Phone,
    string? ManagerName,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record GetStoresQuery(string? SearchTerm, bool? IsActiveOnly) : IRequest<List<StoreDto>>;

public class GetStoresQueryHandler : IRequestHandler<GetStoresQuery, List<StoreDto>>
{
    private readonly IHarnessDbContext _db;
    public GetStoresQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<List<StoreDto>> Handle(GetStoresQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<Store>().AsNoTracking();

        if (request.IsActiveOnly == true)
            query = query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(term) || s.Name.ToLower().Contains(term) || s.Address.ToLower().Contains(term));
        }

        return await query
            .OrderBy(s => s.Code)
            .Select(s => new StoreDto(s.Id, s.Code, s.Name, s.Address, s.Phone, s.ManagerName, s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public record CreateStoreCommand(string Code, string Name, string Address, string Phone, string? ManagerName) : IRequest<Guid>;

public class CreateStoreCommandValidator : AbstractValidator<CreateStoreCommand>
{
    public CreateStoreCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
    }
}

public class CreateStoreCommandHandler : IRequestHandler<CreateStoreCommand, Guid>
{
    private readonly IHarnessDbContext _db;
    public CreateStoreCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Set<Store>().AnyAsync(s => s.Code.ToLower() == request.Code.Trim().ToLower(), cancellationToken);
        if (exists) throw new InvalidOperationException($"Mã cửa hàng '{request.Code}' đã tồn tại.");

        var store = Store.Create(request.Code, request.Name, request.Address, request.Phone, request.ManagerName);
        _db.Set<Store>().Add(store);
        await _db.SaveChangesAsync(cancellationToken);
        return store.Id;
    }
}

public record UpdateStoreCommand(Guid Id, string Name, string Address, string Phone, string? ManagerName, bool IsActive) : IRequest<bool>;

public class UpdateStoreCommandHandler : IRequestHandler<UpdateStoreCommand, bool>
{
    private readonly IHarnessDbContext _db;
    public UpdateStoreCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _db.Set<Store>().FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (store == null) return false;

        store.Update(request.Name, request.Address, request.Phone, request.ManagerName, request.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record DeleteStoreCommand(Guid Id) : IRequest<bool>;

public class DeleteStoreCommandHandler : IRequestHandler<DeleteStoreCommand, bool>
{
    private readonly IHarnessDbContext _db;
    public DeleteStoreCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<bool> Handle(DeleteStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await _db.Set<Store>().FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (store == null) return false;

        _db.Set<Store>().Remove(store);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
