using Microsoft.EntityFrameworkCore;
using Splitly.Api.Domain;

namespace Splitly.Api.Application.Abstractions;

public interface ISplitlyDbContext
{
    DbSet<ExpenseGroup> ExpenseGroups { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
