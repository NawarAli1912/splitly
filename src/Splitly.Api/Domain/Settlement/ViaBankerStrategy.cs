namespace Splitly.Api.Domain.Settlement;

/// <summary>
/// Everyone settles through one hub person: debtors pay the banker, the banker
/// pays creditors. The banker's own balance is implied since balances sum to zero.
/// </summary>
public sealed class ViaBankerStrategy(Guid bankerId) : ISettlementStrategy
{
    public IReadOnlyList<Transfer> Settle(ExpenseGroup group)
    {
        List<Transfer> transfers = [];

        foreach ((Guid participantId, Money balance) in group.NetBalances().OrderBy(b => b.Key))
        {
            if (participantId == bankerId)
            {
                continue;
            }

            if (balance.IsNegative)
            {
                transfers.Add(new Transfer(participantId, bankerId, balance.Abs()));
            }
            else if (balance.IsPositive)
            {
                transfers.Add(new Transfer(bankerId, participantId, balance));
            }
        }

        return transfers;
    }
}
