using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Expenses;

public sealed class RemoveExpenseHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<Deleted>> HandleAsync(
        Guid groupId,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .Include(g => g.Expenses)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var removeResult = group.RemoveExpense(expenseId);

        if (removeResult.IsError)
        {
            return removeResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
