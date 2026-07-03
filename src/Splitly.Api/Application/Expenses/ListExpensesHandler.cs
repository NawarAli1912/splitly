using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Common;
using Splitly.Api.Contracts.Expenses;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Expenses;

public sealed class ListExpensesHandler(ISplitlyDbContext dbContext)
{
    private const int MaxPageSize = 100;

    private static readonly Error InvalidPagination = Error.Validation(
        "Expenses.InvalidPagination", $"Page must be >= 1 and page size between 1 and {MaxPageSize}.");

    public async Task<ErrorOr<PaginatedResponse<ExpenseResponse>>> HandleAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return InvalidPagination;
        }

        var groupExists = await dbContext.ExpenseGroups
            .AnyAsync(g => g.Id == groupId, cancellationToken);

        if (!groupExists)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var expensesQuery = dbContext.ExpenseGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Expenses);

        var totalCount = await expensesQuery.CountAsync(cancellationToken);

        var expenses = await expensesQuery
            .OrderByDescending(e => e.SpentOn)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<ExpenseResponse>(
            expenses.Select(e => e.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount);
    }
}
