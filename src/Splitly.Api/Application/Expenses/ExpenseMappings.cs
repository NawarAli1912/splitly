using Splitly.Api.Contracts.Expenses;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Expenses;

public static class ExpenseMappings
{
    public static ExpenseResponse ToResponse(this Expense expense) =>
        new(
            expense.Id,
            expense.PaidById,
            expense.Amount.Value,
            expense.Description,
            expense.SpentOn,
            expense.SplitAmong);
}
