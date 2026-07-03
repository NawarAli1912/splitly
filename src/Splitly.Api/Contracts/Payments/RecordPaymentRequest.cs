namespace Splitly.Api.Contracts.Payments;

public sealed record RecordPaymentRequest(
    Guid FromParticipantId,
    Guid ToParticipantId,
    decimal Amount,
    DateOnly PaidOn);
