using System.Text.Json;
using FlowOps.Application.Subscriptions;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Health;
using FlowOps.Infrastructure.Idempotency;
using FlowOps.Infrastructure.Messaging;
using FlowOps.Infrastructure.Sql;
using FlowOps.Infrastructure.Sql.Reporting;
using FlowOps.Middleware;
using FlowOps.Pricing;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting.Sql;
using FlowOps.Services.Subscriptions.Sql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var role = (builder.Configuration["FLOWOPS_ROLE"] ?? "api").Trim().ToLowerInvariant();
var isBilling = role == "billing";
var isReporting = role == "reporting";
var isApi = !isBilling && !isReporting;

builder.Services.AddHealthChecks().AddCheck<SqlHealthCheck>("sql-db");

builder.Services.AddDbContext<FlowOpsDbContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("ReportingDb")
        ?? builder.Configuration["ConnectionStrings:ReportingDb"]
        ?? builder.Configuration["ConnectionStrings__ReportingDb"]
        ?? throw new InvalidOperationException(
            "Missing connection string 'ReportingDb'. Set it via appsettings or env: ConnectionStrings__ReportingDb.");

    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IIdempotencyStore, EfCoreIdempotencyStore>();

builder.Services.AddSingleton<IIntegrationEventStore, EfCoreIntegrationEventStore>();

builder.Services.AddSingleton<IPlanPricing, InMemoryPlanPricing>();

builder.Services.AddSingleton<RabbitMqEventBus>();
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var innerBus = sp.GetRequiredService<RabbitMqEventBus>();
    var eventStore = sp.GetRequiredService<IIntegrationEventStore>();
    var logger = sp.GetRequiredService<ILogger<StoringEventBus>>();
    return new StoringEventBus(innerBus, eventStore, logger);
});

if (isBilling)
{
    builder.Services.AddScoped<IBillingHandler, BillingHandler>();
    builder.Services.AddHostedService<BillingListener>();
}

if (isReporting)
{
    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddHostedService<SqlSubscriptionsProjector>();
    builder.Services.AddHostedService<SqlReportingProjector>();

    builder.Services.AddSingleton<EventRecorder>();
    builder.Services.AddHostedService<EventRecorderListener>();

    builder.Services.AddSingleton<ISqlReportingQueries, SqlReportingQueries>();
}

if (isApi)
{
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
    builder.Services.AddScoped<SubscriptionCommandService>();

}

var app = builder.Build();
app.Logger.LogInformation("FLOWOPS_ROLE={Role}", role);

app.UseMiddleware<ProblemDetailsMiddleware>();

if (!string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_HTTPS_PORTS"]))
{
    app.UseHttpsRedirection();
}

if (isApi)
{
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapControllers();
}

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/healthz/details", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message
            })
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(json);
    }
});

app.Run();
