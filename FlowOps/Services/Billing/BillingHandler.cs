using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Pricing;

namespace FlowOps.Services.Billing;

public class BillingHandler : IBillingHandler
{
    private readonly ILogger<BillingHandler> _logger;
    private readonly IEventBus _eventBus;
    private readonly IPlanPricing _planPricing;

    public BillingHandler(ILogger<BillingHandler> logger, IEventBus eventBus, IPlanPricing pricing)
    {
        _logger = logger;
        _eventBus = eventBus;
        _planPricing = pricing;
    }
    private async Task PublishWithRetryAsync(IntegrationEvent ev, int maxRetries = 3, int initialDelayMs = 50)
    {
        var delay = initialDelayMs;
        for(int attempt =1; ; attempt++)
        {
            try
            {
                await _eventBus.PublishAsync(ev);
                return;
            }
            catch (Exception ex) when (attempt <= maxRetries)
            {
                _logger.LogWarning(ex, "Published failed (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}ms...", 
                    attempt, maxRetries, delay);
                await Task.Delay(delay);
                delay *= 2;
            }
        }
    }
    public async Task HandleAsync(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default)
    {
        var amount = _planPricing.GetPrice(ev.PlanCode);

        _logger.LogInformation(
            "BillingHandler: generating invoice for SubscriptionId={SubscriptionId}, CustomerId={CustomerId}, Plan={Plan}, Amount={Amount} PLN",
            ev.SubscriptionId,
            ev.CustomerId,
            ev.PlanCode,
            amount);

        var issued = new InvoiceIssuedEvent
        {
            InvoiceId = Guid.NewGuid(),
            CustomerId = ev.CustomerId,
            SubscriptionId = ev.SubscriptionId,
            PlanCode = ev.PlanCode,
            Amount = amount,
            Currency = "PLN",
            IssuedAt = DateTime.UtcNow
        };
        await PublishWithRetryAsync(issued);
    }
}
