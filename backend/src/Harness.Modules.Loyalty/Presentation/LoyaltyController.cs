using Harness.BuildingBlocks.Presentation;
using Harness.Modules.Loyalty.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Harness.Modules.Loyalty.Presentation;

public class LoyaltyController : ApiController
{
    public LoyaltyController(ISender mediator) : base(mediator) { }

    /// <summary>Tích điểm sau khi đơn hoàn thành.</summary>
    [HttpPost("earn")]
    public async Task<IActionResult> Earn([FromBody] EarnPointsCommand command)
        => Ok(ApiResponse.Ok(await Mediator.Send(command)));

    /// <summary>Đổi điểm lấy quà từ kho quà.</summary>
    [HttpPost("redeem-reward")]
    public async Task<IActionResult> RedeemReward([FromBody] RedeemRewardCommand command)
        => Ok(ApiResponse<object>.Ok(await Mediator.Send(command), "Đã đổi quà thành công."));

    /// <summary>Kho quà đang hoạt động.</summary>
    [HttpGet("rewards")]
    public async Task<IActionResult> GetRewards()
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetRewardsQuery())));

    /// <summary>Xem điểm & hạng của khách.</summary>
    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(Guid customerId)
    {
        var account = await Mediator.Send(new GetLoyaltyQuery(customerId));
        return account is null
            ? NotFound(ApiResponse.Fail("Khách hàng chưa có tài khoản tích điểm."))
            : Ok(ApiResponse.Ok(account));
    }

    /// <summary>Lịch sử cộng/trừ điểm của khách.</summary>
    [HttpGet("{customerId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(Guid customerId)
        => Ok(ApiResponse.Ok(await Mediator.Send(new GetPointTransactionsQuery(customerId))));
}

