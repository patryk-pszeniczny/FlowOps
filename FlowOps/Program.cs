using FlowOps.Application.Common;
using FlowOps.Application.Customers.Commands;
using FlowOps.Application.Customers.Queries;
using FlowOps.Application.Reporting;
using FlowOps.Application.Subscriptions.Commands;
using FlowOps.Application.Subscriptions.Queries;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Customers;
using FlowOps.Domain.Plans;
using FlowOps.Domain.Subscriptions;
using FlowOps.Events;
using FlowOps.Infrastructure.Common;
using FlowOps.Infrastructure.Customers;
using FlowOps.Infrastructure.Health;
using FlowOps.Infrastructure.Idempotency;
using FlowOps.Infrastructure.Messaging;
using FlowOps.Infrastructure.Sql;
using FlowOps.Infrastructure.Sql.Reporting;
using FlowOps.Infrastructure.Subscriptions;
using FlowOps.Middleware;
using FlowOps.Pricing;
using FlowOps.Reports.Stores;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting.Customer;
using FlowOps.Services.Reporting.Sql;
using FlowOps.Services.Subscriptions.Sql;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var role = (builder.Configuration["FLOWOPS_ROLE"] ?? "api").Trim().ToLowerInvariant();
var isBilling = role == "billing";
var isReporting = role == "reporting";
var isApi = !isBilling && !isReporting;
var exposesHttpApi = isApi || isReporting;

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

builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();

builder.Services.AddSingleton<IPlanPricing, InMemoryPlanPricing>();
builder.Services.AddSingleton<IReportingStore, InMemoryReportingStore>();

builder.Services.AddSingleton<RabbitMqEventBus>();
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var innerBus = sp.GetRequiredService<RabbitMqEventBus>();
    var eventStore = sp.GetRequiredService<IIntegrationEventStore>();
    var logger = sp.GetRequiredService<ILogger<StoringEventBus>>();
    return new StoringEventBus(innerBus, eventStore, logger);
});

if (exposesHttpApi)
{
    builder.Services.AddControllers();
}

if (isBilling)
{
    builder.Services.AddScoped<IBillingHandler, BillingHandler>();
    builder.Services.AddHostedService<BillingListener>();
}

if (isReporting)
{
    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

    builder.Services.AddSingleton<IReportingQueries, SqlReportingQueries>();
    builder.Services.AddScoped<CustomerDirectoryQueries>();

    builder.Services.AddScoped<IIntegrationEventHandler<CustomerCreatedEvent>, CustomerCreatedEventHandler>();

    builder.Services.AddHostedService<SqlSubscriptionsProjector>();
    builder.Services.AddHostedService<SqlReportingProjector>();
    builder.Services.AddHostedService<CustomerDirectoryListener>();

    builder.Services.AddSingleton<EventRecorder>();
    builder.Services.AddHostedService<EventRecorderListener>();
}


if (isApi)
{
    builder.Services.AddOpenApi();
    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();

    builder.Services.AddScoped<CreateSubscriptionCommandHandler>();
    builder.Services.AddScoped<CancelSubscriptionCommandHandler>();
    builder.Services.AddScoped<SuspendSubscriptionCommandHandler>();
    builder.Services.AddScoped<ResumeSubscriptionCommandHandler>();
    builder.Services.AddScoped<SubscriptionQueries>();

    builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
    builder.Services.AddScoped<CreateCustomerCommandHandler>();
    builder.Services.AddScoped<CustomerQueries>();

    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddSingleton<IReportingQueries, SqlReportingQueries>();

}

var app = builder.Build();

app.Logger.LogInformation("FLOWOPS_ROLE={Role}", role);

if (isReporting)
{
    using var scope = app.Services.CreateScope();
    var ok = scope.ServiceProvider.GetService<CustomerDirectoryQueries>() is not null;
    app.Logger.LogInformation("DI check: CustomerDirectoryQueries registered = {Ok}", ok);
}

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
}

if (exposesHttpApi)
{
    app.MapControllers();
}

app.MapGet("/whoami", () => Results.Ok(new { role }));

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
