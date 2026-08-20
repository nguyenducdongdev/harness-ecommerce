using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Organization.Application;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Organization.Presentation;

[ApiController]
[Route("api/v1/admin/kpi")]
public class KpiController : ApiController
{
    public KpiController(ISender mediator) : base(mediator) { }

    [HttpGet("targets")]
    [ProducesResponseType(typeof(ApiResponse<List<KpiTargetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTargets([FromQuery] int? month, [FromQuery] int? year, [FromQuery] Guid? staffId)
    {
        var list = await Mediator.Send(new GetKpiTargetsQuery(month, year, staffId));
        return Ok(ApiResponse<List<KpiTargetDto>>.Ok(list));
    }

    [HttpPost("targets")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTarget([FromBody] SetKpiTargetCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Thiết lập chỉ tiêu KPI thành công."));
    }

    [HttpGet("sales-report")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesKpiReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesReport([FromQuery] int month, [FromQuery] int year, [FromQuery] Guid? storeId)
    {
        if (month <= 0) month = DateTime.Now.Month;
        if (year <= 0) year = DateTime.Now.Year;

        var report = await Mediator.Send(new GetSalesKpiReportQuery(month, year, storeId));
        return Ok(ApiResponse<List<SalesKpiReportDto>>.Ok(report));
    }
}
