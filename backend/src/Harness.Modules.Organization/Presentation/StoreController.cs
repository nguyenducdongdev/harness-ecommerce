using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Organization.Application;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Organization.Presentation;

[ApiController]
[Route("api/v1/admin/stores")]
public class StoreController : ApiController
{
    public StoreController(ISender mediator) : base(mediator) { }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StoreDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStores([FromQuery] string? searchTerm, [FromQuery] bool? isActiveOnly)
    {
        var stores = await Mediator.Send(new GetStoresQuery(searchTerm, isActiveOnly));
        return Ok(ApiResponse<List<StoreDto>>.Ok(stores));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreCommand command)
    {
        var id = await Mediator.Send(command);
        return Ok(ApiResponse<Guid>.Ok(id, "Tạo cửa hàng thành công."));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreRequest request)
    {
        var result = await Mediator.Send(new UpdateStoreCommand(id, request.Name, request.Address, request.Phone, request.ManagerName, request.IsActive));
        return Ok(ApiResponse<bool>.Ok(result, "Cập nhật thông tin cửa hàng thành công."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteStore(Guid id)
    {
        var result = await Mediator.Send(new DeleteStoreCommand(id));
        return Ok(ApiResponse<bool>.Ok(result, "Xóa cửa hàng thành công."));
    }
}

public record UpdateStoreRequest(string Name, string Address, string Phone, string? ManagerName, bool IsActive);
