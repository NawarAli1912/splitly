using Microsoft.EntityFrameworkCore;
using Splitly.Api.Domain;

namespace Splitly.Api.Database;

public sealed class SplitlyDbContext(DbContextOptions<SplitlyDbContext> options) : DbContext(options)
{
    public DbSet<ExpenseGroup> ExpenseGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SplitlyDbContext).Assembly);
    }
}
