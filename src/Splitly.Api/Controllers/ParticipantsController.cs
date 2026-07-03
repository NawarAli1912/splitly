using Microsoft.AspNetCore.Mvc;
using Splitly.Api.Application.Participants;
using Splitly.Api.Contracts.Groups;
using Splitly.Api.Contracts.Participants;

namespace Splitly.Api.Controllers;

[Route("groups/{groupId:guid}/participants")]
public sealed class ParticipantsController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<ParticipantResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddParticipant(
        Guid groupId,
        AddParticipantRequest request,
        [FromServices] AddParticipantHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, request, cancellationToken);

        return result.Match(
            participant => Created($"/groups/{groupId}", participant),
            Problem);
    }

    [HttpDelete("{participantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveParticipant(
        Guid groupId,
        Guid participantId,
        [FromServices] RemoveParticipantHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(groupId, participantId, cancellationToken);

        return result.Match(_ => NoContent(), Problem);
    }
}
