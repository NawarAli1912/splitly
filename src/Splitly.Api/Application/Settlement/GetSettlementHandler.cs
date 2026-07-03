using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Settlement;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Settlement;

public sealed class GetSettlementHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<SettlementResponse>> HandleAsync(
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .AsNoTracking()
            .Include(g => g.Expenses)
            .Include(g => g.Payments)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var transfers = group.Settle()
            .Select(t => new TransferResponse(t.FromParticipantId, t.ToParticipantId, t.Amount.Value))
            .ToList();

        return new SettlementResponse(transfers);
    }
}
