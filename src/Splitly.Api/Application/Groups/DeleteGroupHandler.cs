using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Groups;

public sealed class DeleteGroupHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<Deleted>> HandleAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var deletedCount = await dbContext.ExpenseGroups
            .Where(g => g.Id == groupId)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount == 0 ? ExpenseGroupErrors.NotFound : Result.Deleted;
    }
}
