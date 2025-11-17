using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Services;
using FlowOps.Reports.Stores;
using FlowOps.Services.Reporting;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
