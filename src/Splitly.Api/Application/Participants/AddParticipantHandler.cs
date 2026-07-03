using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Application.Groups;
using Splitly.Api.Contracts.Groups;
using Splitly.Api.Contracts.Participants;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Participants;

public sealed class AddParticipantHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<ParticipantResponse>> HandleAsync(
        Guid groupId,
        AddParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var participantResult = group.AddParticipant(request.Name);

        if (participantResult.IsError)
        {
            return participantResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return participantResult.Value.ToResponse();
    }
}
