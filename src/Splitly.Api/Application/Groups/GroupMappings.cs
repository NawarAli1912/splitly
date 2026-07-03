using Splitly.Api.Contracts.Groups;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Groups;

public static class GroupMappings
{
    public static GroupResponse ToResponse(this ExpenseGroup group) =>
        new(
            group.Id,
            group.Name,
            group.Currency,
            group.CreatedAtUtc,
            group.Participants.Select(p => p.ToResponse()).ToList());

    public static ParticipantResponse ToResponse(this Participant participant) =>
        new(participant.Id, participant.Name);
}
