using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Application.Groups;
using Splitly.Api.Contracts.Groups;

namespace Splitly.Api.Controllers;

[Route("groups")]
public sealed class GroupsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<GroupResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup(
        CreateGroupRequest request,
        [FromServices] CreateGroupHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        return result.Match(
            group => CreatedAtAction(nameof(GetGroup), new { groupId = group.Id }, group),
            Problem);
    }

    [HttpGet("{groupId:guid}")]
    [ProducesResponseType<GroupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(
        Guid groupId,
        [FromServices] GetGroupHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, cancellationToken);

        return result.Match(Ok, Problem);
    }

    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(
        Guid groupId,
        [FromServices] DeleteGroupHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, cancellationToken);

        return result.Match(_ => NoContent(), Problem);
    }
}
