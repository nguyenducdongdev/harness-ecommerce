using Harness.BuildingBlocks.Infrastructure.Persistence;
using Harness.Modules.Organization.Domain;
using Harness.Modules.Order.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Harness.Modules.Organization.Application;

public record SalesKpiReportDto(
    Guid? TargetId,
    Guid StaffId,
    string StaffName,
    Guid? StoreId,
    string? StoreName,
    int Month,
    int Year,
    decimal TargetRevenue,
    int TargetOrders,
    decimal ActualRevenue,
    int ActualOrders,
    double RevenueCompletionRate,
    double OrderCompletionRate);

public record GetSalesKpiReportQuery(int Month, int Year, Guid? StoreId) : IRequest<List<SalesKpiReportDto>>;

public class GetSalesKpiReportQueryHandler : IRequestHandler<GetSalesKpiReportQuery, List<SalesKpiReportDto>>
{
    private readonly IHarnessDbContext _db;
    public GetSalesKpiReportQueryHandler(IHarnessDbContext db) => _db = db;

    public async Task<List<SalesKpiReportDto>> Handle(GetSalesKpiReportQuery request, CancellationToken cancellationToken)
    {
        var kpiTargetsQuery = _db.Set<KpiTarget>().AsNoTracking()
            .Where(k => k.Month == request.Month && k.Year == request.Year);

        if (request.StoreId.HasValue)
        {
            kpiTargetsQuery = kpiTargetsQuery.Where(k => k.StoreId == request.StoreId.Value);
        }

        var targets = await kpiTargetsQuery.ToListAsync(cancellationToken);

        var startDate = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = startDate.AddMonths(1);

        var orders = await _db.Set<Harness.Modules.Order.Domain.Order>().AsNoTracking()
            .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate)
            .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed)
            .ToListAsync(cancellationToken);

        var ordersByCreatedBy = orders
            .GroupBy(o => o.CreatedBy?.Trim().ToLower() ?? "system")
            .ToDictionary(
                g => g.Key,
                g => new { Revenue = g.Sum(x => x.TotalAmount), Count = g.Count() }
            );

        var report = new List<SalesKpiReportDto>();

        foreach (var target in targets)
        {
            var staffKey = target.StaffName.Trim().ToLower();
            ordersByCreatedBy.TryGetValue(staffKey, out var actual);

            var actualRev = actual?.Revenue ?? 0m;
            var actualCount = actual?.Count ?? 0;

            var revRate = target.TargetRevenue > 0 ? (double)(actualRev / target.TargetRevenue * 100m) : 0;
            var orderRate = target.TargetOrders > 0 ? (double)((decimal)actualCount / target.TargetOrders * 100m) : 0;

            report.Add(new SalesKpiReportDto(
                target.Id,
                target.StaffId,
                target.StaffName,
                target.StoreId,
                target.StoreName,
                target.Month,
                target.Year,
                target.TargetRevenue,
                target.TargetOrders,
                actualRev,
                actualCount,
                Math.Round(revRate, 2),
                Math.Round(orderRate, 2)
            ));
        }

        foreach (var kvp in ordersByCreatedBy)
        {
            if (!targets.Any(t => t.StaffName.Trim().Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
            {
                report.Add(new SalesKpiReportDto(
                    null,
                    Guid.Empty,
                    kvp.Key,
                    null,
                    null,
                    request.Month,
                    request.Year,
                    0m,
                    0,
                    kvp.Value.Revenue,
                    kvp.Value.Count,
                    100.0,
                    100.0
                ));
            }
        }

        return report.OrderByDescending(r => r.ActualRevenue).ToList();
    }
}
