using FlowOps.Application.Subscriptions;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Health;
using FlowOps.Infrastructure.Idempotency;
using FlowOps.Infrastructure.Sql;
using FlowOps.Infrastructure.Sql.Reporting;
using FlowOps.Middleware;
using FlowOps.Pricing;
using FlowOps.Reports.Stores;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting;
using FlowOps.Services.Reporting.Sql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IReportingStore, InMemoryReportingStore>();

//billing
builder.Services.AddScoped<IBillingHandler, BillingHandler>();
builder.Services.AddHostedService<BillingListener>();

//Reporting
builder.Services.AddScoped<IReportingHandler, ReportingHandler>();
builder.Services.AddHostedService<ReportingListener>();

//Subscriptions
builder.Services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
builder.Services.AddScoped<SubscriptionCommandService>();

//Replay
builder.Services.AddSingleton<EventRecorder>();
builder.Services.AddHostedService<EventRecorderListener>();

//Pricing
builder.Services.AddSingleton<IPlanPricing, InMemoryPlanPricing>();

//Idempotency
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

//Database
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddHostedService<SqlReportingProjector>();
builder.Services.AddSingleton<ISqlReportingQueries, SqlReportingQueries>();

//DataBase - Health Check
builder.Services.AddHealthChecks().AddCheck<SqlHealthCheck>("sql-db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.MapControllers();
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
