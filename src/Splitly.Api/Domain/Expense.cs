namespace Splitly.Api.Domain;

public sealed class Expense
{
    private readonly List<Guid> _splitAmong;

    public Guid Id { get; private set; }

    public Guid PaidById { get; private set; }

    public Money Amount { get; private set; }

    public string Description { get; private set; }

    public DateOnly SpentOn { get; private set; }

    public IReadOnlyList<Guid> SplitAmong => _splitAmong;

    internal Expense(
        Guid id,
        Guid paidById,
        Money amount,
        string description,
        DateOnly spentOn,
        List<Guid> splitAmong)
    {
        Id = id;
        PaidById = paidById;
        Amount = amount;
        Description = description;
        SpentOn = spentOn;
        _splitAmong = splitAmong;
    }

    public IReadOnlyList<(Guid ParticipantId, Money Share)> Shares()
    {
        var shares = Amount.SplitEvenly(_splitAmong.Count);

        return _splitAmong.Select((participantId, i) => (participantId, shares[i])).ToList();
    }

    public bool Involves(Guid participantId) =>
        PaidById == participantId || _splitAmong.Contains(participantId);

    private Expense()
    {
        Description = null!;
        _splitAmong = null!;
    }
}
