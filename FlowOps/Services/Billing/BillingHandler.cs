using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Services.Billing;

public class BillingHandler : IBillingHandler
{
    private readonly ILogger<BillingHandler> _logger;
    private readonly IEventBus _eventBus;

    public BillingHandler(ILogger<BillingHandler> logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task HandleAsync(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default)
    {
        var amount = ResolveAmount(ev.PlanCode);

        _logger.LogInformation(
            "BillingHandler: generating invoice for SubscriptionId={SubscriptionId}, CustomerId={CustomerId}, Plan={Plan}, Amount={Amount} PLN",
            ev.SubscriptionId,
            ev.CustomerId,
            ev.PlanCode,
            amount);

        var issued = new InvoiceIssuedEvent
        {
            InoiceId = Guid.NewGuid(),
            CustomerId = ev.CustomerId,
            SubscriptionId = ev.SubscriptionId,
            PlanCode = ev.PlanCode,
            Amount = amount,
            Currency = "PLN",
            IssuedAt = DateTime.UtcNow
        };
        await _eventBus.PublishAsync(issued);
    }
    private static decimal ResolveAmount(string? planCode) =>
        (planCode ?? string.Empty).ToUpperInvariant() switch
        {
            "PRO" => 99m,
            "BUSINESS" => 199m,
            "ENTERPRISE" => 499m,
            _ => 49m // Default plan
        };
}
