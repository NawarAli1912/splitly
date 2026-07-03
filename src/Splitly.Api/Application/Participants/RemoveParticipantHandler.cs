using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Participants;

public sealed class RemoveParticipantHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<Deleted>> HandleAsync(
        Guid groupId,
        Guid participantId,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .Include(g => g.Participants)
            .Include(g => g.Expenses)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var removeResult = group.RemoveParticipant(participantId);

        if (removeResult.IsError)
        {
            return removeResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
