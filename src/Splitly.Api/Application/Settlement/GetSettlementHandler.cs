using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Settlement;
using Splitly.Api.Domain;
using Splitly.Api.Domain.Settlement;

namespace Splitly.Api.Application.Settlement;

public sealed class GetSettlementHandler(ISplitlyDbContext dbContext)
{
    private static readonly Error UnknownStrategy = Error.Validation(
        "Settlement.UnknownStrategy",
        "Strategy must be one of: minimum-transfers, direct-payback, via-banker.");

    private static readonly Error HubRequired = Error.Validation(
        "Settlement.HubRequired", "The via-banker strategy requires a hub participant id.");

    public async Task<ErrorOr<SettlementResponse>> HandleAsync(
        Guid groupId,
        string? strategy,
        Guid? hub,
        CancellationToken cancellationToken)
    {
        var group = await dbContext.ExpenseGroups
            .AsNoTracking()
            .Include(g => g.Participants)
            .Include(g => g.Expenses)
            .Include(g => g.Payments)
            .SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            return ExpenseGroupErrors.NotFound;
        }

        var strategyResult = ResolveStrategy(group, strategy, hub);

        if (strategyResult.IsError)
        {
            return strategyResult.Errors;
        }

        var transfers = group.Settle(strategyResult.Value)
            .Select(t => new TransferResponse(t.FromParticipantId, t.ToParticipantId, t.Amount.Value))
            .ToList();

        return new SettlementResponse(transfers);
    }

    private static ErrorOr<ISettlementStrategy> ResolveStrategy(
        ExpenseGroup group,
        string? strategy,
        Guid? hub)
    {
        switch (strategy)
        {
            case null or "minimum-transfers":
                return new MinimumTransfersStrategy();

            case "direct-payback":
                return new DirectPaybackStrategy();

            case "via-banker":
                if (hub is null)
                {
                    return HubRequired;
                }

                if (!group.Participants.Any(p => p.Id == hub.Value))
                {
                    return ExpenseGroupErrors.ParticipantNotFound;
                }

                return new ViaBankerStrategy(hub.Value);

            default:
                return UnknownStrategy;
        }
    }
}
