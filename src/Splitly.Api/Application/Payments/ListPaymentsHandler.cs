using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Common;
using Splitly.Api.Contracts.Payments;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Payments;

public sealed class ListPaymentsHandler(ISplitlyDbContext dbContext)
{
    private const int MaxPageSize = 100;

    private static readonly Error InvalidPagination = Error.Validation(
        "Payments.InvalidPagination", $"Page must be >= 1 and page size between 1 and {MaxPageSize}.");

    public async Task<ErrorOr<PaginatedResponse<PaymentResponse>>> HandleAsync(
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

        var paymentsQuery = dbContext.ExpenseGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .SelectMany(g => g.Payments);

        var totalCount = await paymentsQuery.CountAsync(cancellationToken);

        var payments = await paymentsQuery
            .OrderByDescending(p => p.PaidOn)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PaymentResponse>(
            payments.Select(p => p.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount);
    }
}
