using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.BuildingBlocks.Presentation;

/// <summary>Base controller: route /api/v1/[controller], gửi request qua MediatR.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    protected readonly ISender Mediator;

    protected ApiController(ISender mediator) => Mediator = mediator;
}
