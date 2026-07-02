using ErrorOr;

namespace Splitly.Api.Domain;

public sealed class ExpenseGroup
{
    private readonly List<Participant> _participants = [];
    private readonly List<Expense> _expenses = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Currency { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<Participant> Participants => _participants;

    public IReadOnlyCollection<Expense> Expenses => _expenses;

    private ExpenseGroup(Guid id, string name, string currency, DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Currency = currency;
        CreatedAtUtc = createdAtUtc;
    }

    public static ErrorOr<ExpenseGroup> Create(string name, string currency)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ExpenseGroupErrors.NameRequired;
        }

        if (currency.Length != 3 || !currency.All(char.IsLetter))
        {
            return ExpenseGroupErrors.InvalidCurrency;
        }

        return new ExpenseGroup(
            Guid.NewGuid(),
            name.Trim(),
            currency.ToUpperInvariant(),
            DateTime.UtcNow);
    }

    public ErrorOr<Participant> AddParticipant(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ExpenseGroupErrors.ParticipantNameRequired;
        }

        var trimmedName = name.Trim();

        if (_participants.Any(p => string.Equals(p.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return ExpenseGroupErrors.DuplicateParticipant;
        }

        var participant = new Participant(Guid.NewGuid(), trimmedName);
        _participants.Add(participant);

        return participant;
    }

    public ErrorOr<Deleted> RemoveParticipant(Guid participantId)
    {
        var participant = _participants.FirstOrDefault(p => p.Id == participantId);

        if (participant is null)
        {
            return ExpenseGroupErrors.ParticipantNotFound;
        }

        if (_expenses.Any(e => e.Involves(participantId)))
        {
            return ExpenseGroupErrors.ParticipantHasExpenses;
        }

        _participants.Remove(participant);

        return Result.Deleted;
    }

    public ErrorOr<Expense> AddExpense(
        Guid paidBy,
        decimal amount,
        string description,
        DateOnly spentOn,
        IReadOnlyList<Guid> splitAmong)
    {
        if (amount <= 0)
        {
            return ExpenseGroupErrors.InvalidAmount;
        }

        var amountResult = Money.FromDecimal(amount);

        if (amountResult.IsError)
        {
            return amountResult.Errors;
        }

        var splitParticipants = splitAmong.Distinct().ToList();

        if (splitParticipants.Count == 0)
        {
            return ExpenseGroupErrors.EmptySplit;
        }

        if (!IsParticipant(paidBy) || !splitParticipants.All(IsParticipant))
        {
            return ExpenseGroupErrors.ParticipantNotFound;
        }

        var expense = new Expense(Guid.NewGuid(), paidBy, amountResult.Value, description, spentOn, splitParticipants);
        _expenses.Add(expense);

        return expense;
    }

    public ErrorOr<Deleted> RemoveExpense(Guid expenseId)
    {
        var removedCount = _expenses.RemoveAll(e => e.Id == expenseId);

        return removedCount == 0 ? ExpenseGroupErrors.ExpenseNotFound : Result.Deleted;
    }

    public IReadOnlyList<Transfer> Settle()
    {
        Dictionary<Guid, Money> participantsBalances = [];

        foreach (Expense expense in _expenses)
        {
            participantsBalances.TryGetValue(expense.PaidById, out Money balance);
            balance += expense.Amount;
            participantsBalances[expense.PaidById] = balance;

            foreach ((Guid sharedWithId, Money sharedAmount) in expense.Shares())
            {
                participantsBalances.TryGetValue(sharedWithId, out balance);
                balance -= sharedAmount;
                participantsBalances[sharedWithId] = balance;
            }
        }

        var largestFirst = Comparer<Money>.Create((left, right) => right.CompareTo(left));

        PriorityQueue<Guid, Money> debtors = new(largestFirst);
        PriorityQueue<Guid, Money> creditors = new(largestFirst);

        foreach ((Guid participantId, Money balance) in participantsBalances)
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

    private bool IsParticipant(Guid id) => _participants.Any(p => p.Id == id);
}
