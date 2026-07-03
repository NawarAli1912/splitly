namespace Splitly.Api.Domain.Settlement;

/// <summary>
/// Everyone pays back exactly whoever covered them: per-expense shares become
/// pairwise debts, recorded payments reduce them, reciprocal debts cancel.
/// More transfers than the minimum, but every transfer is explainable.
/// </summary>
public sealed class DirectPaybackStrategy : ISettlementStrategy
{
    public IReadOnlyList<Transfer> Settle(ExpenseGroup group)
    {
        Dictionary<(Guid Debtor, Guid Creditor), Money> owed = [];

        foreach (Expense expense in group.Expenses)
        {
            foreach ((Guid participantId, Money share) in expense.Shares())
            {
                if (participantId == expense.PaidById)
                {
                    continue;
                }

                owed.TryGetValue((participantId, expense.PaidById), out Money debt);
                owed[(participantId, expense.PaidById)] = debt + share;
            }
        }

        foreach (Payment payment in group.Payments)
        {
            owed.TryGetValue((payment.FromParticipantId, payment.ToParticipantId), out Money debt);
            owed[(payment.FromParticipantId, payment.ToParticipantId)] = debt - payment.Amount;
        }

        List<Transfer> transfers = [];

        foreach (((Guid debtor, Guid creditor), Money debt) in owed.OrderBy(pair => pair.Key.Debtor).ThenBy(pair => pair.Key.Creditor))
        {
            if (debtor.CompareTo(creditor) > 0 && owed.ContainsKey((creditor, debtor)))
            {
                continue; // already netted from the other direction
            }

            owed.TryGetValue((creditor, debtor), out Money reverse);
            Money net = debt - reverse;

            if (net.IsPositive)
            {
                transfers.Add(new Transfer(debtor, creditor, net));
            }
            else if (net.IsNegative)
            {
                transfers.Add(new Transfer(creditor, debtor, net.Abs()));
            }
        }

        return transfers;
    }
}
