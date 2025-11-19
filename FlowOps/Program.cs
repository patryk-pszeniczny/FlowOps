using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Reports.Stores;
using FlowOps.Services.Reporting;
using FlowOps.Domain.Subscriptions;
using FlowOps.Application.Subscriptions;
using FlowOps.Services.Billing;
using FlowOps.Services.Replay;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
builder.Services.AddSingleton<EventRecoder>();
builder.Services.AddHostedService<EventRecoderListener>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
