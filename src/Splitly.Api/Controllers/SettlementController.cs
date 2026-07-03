using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Application.Settlement;
using Splitly.Api.Contracts.Settlement;

namespace Splitly.Api.Controllers;

[Route("groups/{groupId:guid}/settlement")]
public sealed class SettlementController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<SettlementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettlement(
        Guid groupId,
        [FromServices] GetSettlementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, cancellationToken);

        return result.Match(Ok, Problem);
    }
}
