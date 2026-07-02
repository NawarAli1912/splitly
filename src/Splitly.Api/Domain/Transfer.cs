namespace Splitly.Api.Domain;

public sealed record Transfer(Guid FromParticipantId, Guid ToParticipantId, Money Amount);
