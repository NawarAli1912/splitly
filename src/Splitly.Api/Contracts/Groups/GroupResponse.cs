namespace Splitly.Api.Contracts.Groups;

public sealed record GroupResponse(
    Guid Id,
    string Name,
    string Currency,
    DateTime CreatedAtUtc,
    IReadOnlyList<ParticipantResponse> Participants);

public sealed record ParticipantResponse(Guid Id, string Name);
