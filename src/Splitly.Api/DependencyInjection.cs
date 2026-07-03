using Microsoft.EntityFrameworkCore;
using Splitly.Api.Application.Abstractions;
using Splitly.Api.Application.Expenses;
using Splitly.Api.Application.Groups;
using Splitly.Api.Application.Participants;
using Splitly.Api.Application.Payments;
using Splitly.Api.Application.Settlement;
using Splitly.Api.Database;
using Splitly.Api.Middleware;

namespace Splitly.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(options => options.ReturnHttpNotAcceptable = true);

        builder.Services.AddOpenApi();

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier));

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<SplitlyDbContext>(options =>
            options
                .UseNpgsql(builder.Configuration.GetConnectionString("Database"))
                .UseSnakeCaseNamingConvention());

        builder.Services.AddScoped<ISplitlyDbContext>(sp => sp.GetRequiredService<SplitlyDbContext>());

        return builder;
    }

    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<CreateGroupHandler>();
        builder.Services.AddScoped<GetGroupHandler>();
        builder.Services.AddScoped<DeleteGroupHandler>();
        builder.Services.AddScoped<AddParticipantHandler>();
        builder.Services.AddScoped<RemoveParticipantHandler>();
        builder.Services.AddScoped<AddExpenseHandler>();
        builder.Services.AddScoped<ListExpensesHandler>();
        builder.Services.AddScoped<RemoveExpenseHandler>();
        builder.Services.AddScoped<RecordPaymentHandler>();
        builder.Services.AddScoped<ListPaymentsHandler>();
        builder.Services.AddScoped<RemovePaymentHandler>();
        builder.Services.AddScoped<GetSettlementHandler>();

        return builder;
    }
}
