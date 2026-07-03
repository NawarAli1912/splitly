namespace Splitly.Api.Contracts.Expenses;

public sealed record ExpenseResponse(
    Guid Id,
    Guid PaidById,
    decimal Amount,
    string Description,
    DateOnly SpentOn,
    IReadOnlyList<Guid> SplitAmong);
