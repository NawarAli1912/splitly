using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Domain;

namespace Splitly.Api.Database;

public sealed class SplitlyDbContext(DbContextOptions<SplitlyDbContext> options)
    : DbContext(options), ISplitlyDbContext
{
    public DbSet<ExpenseGroup> ExpenseGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SplitlyDbContext).Assembly);
    }
}
