using Microsoft.EntityFrameworkCore;
using Splitly.Api.Database;

namespace Splitly.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SplitlyDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
