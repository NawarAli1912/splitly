namespace Splitly.Api.Domain.Settlement;

/// <summary>
/// Min cash flow: net balances, then greedily match the largest debtor with the
/// largest creditor via two max-heaps. At most n−1 transfers, O(n log n).
/// </summary>
public sealed class MinimumTransfersStrategy : ISettlementStrategy
{
    public IReadOnlyList<Transfer> Settle(ExpenseGroup group)
    {
        var largestFirst = Comparer<Money>.Create((left, right) => right.CompareTo(left));

        PriorityQueue<Guid, Money> debtors = new(largestFirst);
        PriorityQueue<Guid, Money> creditors = new(largestFirst);

        foreach ((Guid participantId, Money balance) in group.NetBalances())
        {
            if (balance.IsNegative)
            {
                debtors.Enqueue(participantId, balance.Abs());
            }
            else if (balance.IsPositive)
            {
                creditors.Enqueue(participantId, balance);
            }
        }

        List<Transfer> transfers = [];

        while (debtors.Count > 0 && creditors.Count > 0)
        {
            debtors.TryDequeue(out Guid debtorId, out Money owed);
            creditors.TryDequeue(out Guid creditorId, out Money credit);

            Money paid = Money.Min(owed, credit);
            transfers.Add(new Transfer(debtorId, creditorId, paid));

            Money remainingDebt = owed - paid;
            Money remainingCredit = credit - paid;

            if (remainingDebt.IsPositive)
            {
                debtors.Enqueue(debtorId, remainingDebt);
            }

            if (remainingCredit.IsPositive)
            {
                creditors.Enqueue(creditorId, remainingCredit);
            }
        }

        return transfers;
    }
}
