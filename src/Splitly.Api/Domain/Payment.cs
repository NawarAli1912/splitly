namespace Splitly.Api.Domain;

public sealed class Payment
{
    public Guid Id { get; private set; }

    public Guid FromParticipantId { get; private set; }

    public Guid ToParticipantId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly PaidOn { get; private set; }

    internal Payment(Guid id, Guid fromParticipantId, Guid toParticipantId, Money amount, DateOnly paidOn)
    {
        Id = id;
        FromParticipantId = fromParticipantId;
        ToParticipantId = toParticipantId;
        Amount = amount;
        PaidOn = paidOn;
    }

    public bool Involves(Guid participantId) =>
        FromParticipantId == participantId || ToParticipantId == participantId;

    private Payment()
    {
    }
}
