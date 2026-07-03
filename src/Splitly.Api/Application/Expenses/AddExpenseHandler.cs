using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Expenses;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Expenses;

public sealed class AddExpenseHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<ExpenseResponse>> HandleAsync(
        Guid groupId,
        AddExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var expenseResult = group.AddExpense(
            request.PaidById,
            request.Amount,
            request.Description,
            request.SpentOn,
            request.SplitAmong);

        if (expenseResult.IsError)
        {
            return expenseResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return expenseResult.Value.ToResponse();
    }
}
