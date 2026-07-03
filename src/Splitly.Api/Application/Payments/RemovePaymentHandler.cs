using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Payments;

public sealed class RemovePaymentHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<Deleted>> HandleAsync(
        Guid groupId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .Include(g => g.Payments)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var removeResult = group.RemovePayment(paymentId);

        if (removeResult.IsError)
        {
            return removeResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
