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
using FlowOps.Reports.Stores;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting;
using FlowOps.Services.Reporting.Sql;
using FlowOps.Services.Subscriptions.Sql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var role = (builder.Configuration["FLOWOPS_ROLE"] ?? "api").Trim().ToLowerInvariant();
var isBilling = role == "billing";
var isApi = !isBilling;

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

builder.Services.AddSingleton<RabbitMqEventBus>();
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var innerBus = sp.GetRequiredService<RabbitMqEventBus>();
    var eventStore = sp.GetRequiredService<IIntegrationEventStore>();
    var logger = sp.GetRequiredService<ILogger<StoringEventBus>>();
    return new StoringEventBus(innerBus, eventStore, logger);
});

builder.Services.AddSingleton<IPlanPricing, InMemoryPlanPricing>();

//BILLING ROLE
if (isBilling)
{
    builder.Services.AddScoped<IBillingHandler, BillingHandler>();
    builder.Services.AddHostedService<BillingListener>();

}
else
{
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddSingleton<IReportingStore, InMemoryReportingStore>();
    builder.Services.AddScoped<IReportingHandler, ReportingHandler>();
    builder.Services.AddHostedService<ReportingListener>();

    builder.Services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
    builder.Services.AddScoped<SubscriptionCommandService>();
    builder.Services.AddHostedService<SqlSubscriptionsProjector>();

    builder.Services.AddSingleton<EventRecorder>();
    builder.Services.AddHostedService<EventRecorderListener>();

    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddHostedService<SqlReportingProjector>();
    builder.Services.AddSingleton<ISqlReportingQueries, SqlReportingQueries>();

}

var app = builder.Build();

app.Logger.LogInformation("FLOWOPS_ROLE={Role}", role);

app.UseMiddleware<ProblemDetailsMiddleware>();

if (!string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_HTTPS_PORTS"]))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

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

if (isApi)
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapControllers();
}

app.Run();
