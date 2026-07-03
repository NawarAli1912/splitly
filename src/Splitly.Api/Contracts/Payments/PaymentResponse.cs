namespace Splitly.Api.Contracts.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid FromParticipantId,
    Guid ToParticipantId,
    decimal Amount,
    DateOnly PaidOn);
