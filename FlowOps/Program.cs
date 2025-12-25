using FlowOps.Application.Common;
using FlowOps.Application.Customers.Commands;
using FlowOps.Application.Customers.Queries;
using FlowOps.Application.Mapping;
using FlowOps.Application.Reporting;
using FlowOps.Application.Subscriptions.Commands;
using FlowOps.Application.Subscriptions.Events;
using FlowOps.Application.Subscriptions.Queries;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Customers;
using FlowOps.Domain.Plans;
using FlowOps.Domain.Subscriptions;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;
using FlowOps.Infrastructure.Common;
using FlowOps.Infrastructure.Health;
using FlowOps.Infrastructure.Idempotency;
using FlowOps.Infrastructure.Messaging;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Inbox;
using FlowOps.Infrastructure.Persistence.Outbox;
using FlowOps.Infrastructure.Persistence.Reporting;
using FlowOps.Infrastructure.Persistence.Repositories;
using FlowOps.Middleware;
using FlowOps.Pricing;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting.Customer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var role = (builder.Configuration["FLOWOPS_ROLE"] ?? "api").Trim().ToLowerInvariant();
var isBilling = role == "billing";
var isReporting = role == "reporting";
var isApi = !isBilling && !isReporting;
var exposesHttpApi = isApi || isReporting;

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());


builder.Services.AddHealthChecks().AddCheck<SqlHealthCheck>("sql-db");

builder.Services.AddDbContext<FlowOpsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("FlowOpsDatabase")
       ?? throw new InvalidOperationException("Missing connection string 'FlowOpsDatabase'. Configure it in appsettings or env ConnectionStrings__FlowOpsDatabase.");

    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IIdempotencyStore, EfCoreIdempotencyStore>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<CustomerDirectoryQueries>();

builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IOutboxMessageWriter, OutboxMessageWriter>();
builder.Services.AddScoped<IIntegrationEventInbox, IntegrationEventInBox>();

builder.Services.AddSingleton<IIntegrationEventStore, EfCoreIntegrationEventStore>();

builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();

builder.Services.AddSingleton<IPlanPricing, InMemoryPlanPricing>();

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
    builder.Services.AddScoped<IReportingQueries, EfReportingQueries>();

    builder.Services.AddScoped<IIntegrationEventHandler<CustomerCreatedEvent>, CustomerCreatedEventHandler>();

    builder.Services.AddHostedService<EfReportingProjector>();
    builder.Services.AddHostedService<CustomerDirectoryListener>();

    builder.Services.AddSingleton<EventRecorder>();
    builder.Services.AddHostedService<EventRecorderListener>();
}


if (isApi)
{
    builder.Services.AddOpenApi();
    builder.Services.AddAuthorization();

    builder.Services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();

    builder.Services.AddScoped<CreateSubscriptionCommandHandler>();
    builder.Services.AddScoped<CancelSubscriptionCommandHandler>();
    builder.Services.AddScoped<SuspendSubscriptionCommandHandler>();
    builder.Services.AddScoped<ResumeSubscriptionCommandHandler>();

    builder.Services.AddScoped<IDomainEventHandler<SubscriptionActivatedDomainEvent>, SubscriptionActivatedDomainEventHandler>();
    builder.Services.AddScoped<IDomainEventHandler<SubscriptionCancelledDomainEvent>, SubscriptionCancelledDomainEventHandler>();
    builder.Services.AddScoped<IDomainEventHandler<SubscriptionSuspendedDomainEvent>, SubscriptionSuspendedDomainEventHandler>();
    builder.Services.AddScoped<IDomainEventHandler<SubscriptionResumedDomainEvent>, SubscriptionResumedDomainEventHandler>();

    builder.Services.AddScoped<SubscriptionQueries>();

    builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
    builder.Services.AddScoped<CreateCustomerCommandHandler>();
    builder.Services.AddScoped<CustomerQueries>();

    builder.Services.AddScoped<IReportingQueries, EfReportingQueries>();
    builder.Services.AddHostedService<EfReportingProjector>();

    builder.Services.AddHostedService<OutboxMessageProcessor>();

}

var app = builder.Build();

app.Logger.LogInformation("FLOWOPS_ROLE={Role}", role);

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();

Console.WriteLine(db.Database.GetDbConnection().ConnectionString);

var applied = await db.Database.GetAppliedMigrationsAsync();
var pending = await db.Database.GetPendingMigrationsAsync();

Console.WriteLine("Applied: " + string.Join(", ", applied));
Console.WriteLine("Pending: " + string.Join(", ", pending));

Console.WriteLine("Known migrations: " + string.Join(", ", db.Database.GetMigrations()));
Console.WriteLine("Applied migrations: " + string.Join(", ", await db.Database.GetAppliedMigrationsAsync()));
Console.WriteLine("Pending migrations: " + string.Join(", ", await db.Database.GetPendingMigrationsAsync()));


await db.Database.MigrateAsync();

if (isReporting)
{
    using var scope_queries = app.Services.CreateScope();
    var ok = scope_queries.ServiceProvider.GetService<CustomerDirectoryQueries>() is not null;
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
