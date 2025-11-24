using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Reports.Stores;
using FlowOps.Services.Reporting;
using FlowOps.Domain.Subscriptions;
using FlowOps.Application.Subscriptions;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;
using FlowOps.Middleware;
using FlowOps.Pricing;
using FlowOps.Infrastructure.Idempotency;
using FlowOps.Infrastructure.Sql;
using FlowOps.Services.Reporting.Sql;

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

app.Run();
