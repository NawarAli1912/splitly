using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Groups;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Groups;

public sealed class GetGroupHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<GroupResponse>> HandleAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .AsNoTracking()
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        return group.ToResponse();
    }
}
