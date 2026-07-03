using ErrorOr;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Contracts.Groups;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Groups;

public sealed class CreateGroupHandler(ISplitlyDbContext dbContext)
{
    public async Task<ErrorOr<GroupResponse>> HandleAsync(
        CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var groupResult = ExpenseGroup.Create(request.Name, request.Currency);

        if (groupResult.IsError)
        {
            return groupResult.Errors;
        }

        dbContext.ExpenseGroups.Add(groupResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return groupResult.Value.ToResponse();
    }
}
