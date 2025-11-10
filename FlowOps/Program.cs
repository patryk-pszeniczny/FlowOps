using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

builder.Services.AddScoped<IBillingHandler, BillingHandler>();
builder.Services.AddHostedService<BillingListener>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
