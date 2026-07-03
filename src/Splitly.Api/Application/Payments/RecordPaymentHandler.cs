using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Payments;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Payments;

public sealed class RecordPaymentHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<PaymentResponse>> HandleAsync(
        Guid groupId,
        RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var paymentResult = group.RecordPayment(
            request.FromParticipantId,
            request.ToParticipantId,
            request.Amount,
            request.PaidOn);

        if (paymentResult.IsError)
        {
            return paymentResult.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return paymentResult.Value.ToResponse();
    }
}
