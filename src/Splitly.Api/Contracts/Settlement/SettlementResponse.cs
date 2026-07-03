namespace Splitly.Api.Contracts.Settlement;

public sealed record SettlementResponse(IReadOnlyList<TransferResponse> Transfers);

public sealed record TransferResponse(Guid FromParticipantId, Guid ToParticipantId, decimal Amount);
