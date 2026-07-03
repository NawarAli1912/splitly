namespace Splitly.Api.Contracts.Expenses;

public sealed record AddExpenseRequest(
    Guid PaidById,
    decimal Amount,
    string Description,
    DateOnly SpentOn,
    IReadOnlyList<Guid> SplitAmong);
