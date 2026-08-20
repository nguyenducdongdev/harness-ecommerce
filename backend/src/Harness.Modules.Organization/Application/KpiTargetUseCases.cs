using FluentValidation;
using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Organization.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Organization.Application;

public record KpiTargetDto(
    Guid Id,
    Guid StaffId,
    string StaffName,
    Guid? StoreId,
    string? StoreName,
    int Month,
    int Year,
    decimal TargetRevenue,
    int TargetOrders,
    string? Notes);

public record GetKpiTargetsQuery(int? Month, int? Year, Guid? StaffId) : IRequest<List<KpiTargetDto>>;

public class GetKpiTargetsQueryHandler : IRequestHandler<GetKpiTargetsQuery, List<KpiTargetDto>>
{
    private readonly IHarnessDbContext _db;
    public GetKpiTargetsQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<List<KpiTargetDto>> Handle(GetKpiTargetsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Set<KpiTarget>().AsNoTracking();

        if (request.Month.HasValue) query = query.Where(k => k.Month == request.Month.Value);
        if (request.Year.HasValue) query = query.Where(k => k.Year == request.Year.Value);
        if (request.StaffId.HasValue) query = query.Where(k => k.StaffId == request.StaffId.Value);

        return await query
            .OrderByDescending(k => k.Year)
            .ThenByDescending(k => k.Month)
            .ThenBy(k => k.StaffName)
            .Select(k => new KpiTargetDto(
                k.Id, k.StaffId, k.StaffName, k.StoreId, k.StoreName,
                k.Month, k.Year, k.TargetRevenue, k.TargetOrders, k.Notes))
            .ToListAsync(cancellationToken);
    }
}

public record SetKpiTargetCommand(
    Guid? Id,
    Guid StaffId,
    string StaffName,
    Guid? StoreId,
    string? StoreName,
    int Month,
    int Year,
    decimal TargetRevenue,
    int TargetOrders,
    string? Notes) : IRequest<Guid>;

public class SetKpiTargetCommandValidator : AbstractValidator<SetKpiTargetCommand>
{
    public SetKpiTargetCommandValidator()
    {
        RuleFor(x => x.StaffName).NotEmpty();
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).GreaterThanOrEqualTo(2020);
        RuleFor(x => x.TargetRevenue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetOrders).GreaterThanOrEqualTo(0);
    }
}

public class SetKpiTargetCommandHandler : IRequestHandler<SetKpiTargetCommand, Guid>
{
    private readonly IHarnessDbContext _db;
    public SetKpiTargetCommandHandler(IHarnessDbContext db) => _db = db;

    public async Task<Guid> Handle(SetKpiTargetCommand request, CancellationToken cancellationToken)
    {
        if (request.Id.HasValue)
        {
            var existing = await _db.Set<KpiTarget>().FirstOrDefaultAsync(k => k.Id == request.Id.Value, cancellationToken);
            if (existing != null)
            {
                existing.Update(request.TargetRevenue, request.TargetOrders, request.Notes);
                await _db.SaveChangesAsync(cancellationToken);
                return existing.Id;
            }
        }

        var target = KpiTarget.Create(
            request.StaffId, request.StaffName, request.StoreId, request.StoreName,
            request.Month, request.Year, request.TargetRevenue, request.TargetOrders, request.Notes);

        _db.Set<KpiTarget>().Add(target);
        await _db.SaveChangesAsync(cancellationToken);
        return target.Id;
    }
}
