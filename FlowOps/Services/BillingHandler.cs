using FlowOps.Events;
using Microsoft.Extensions.Logging;

namespace FlowOps.Services;

public class BillingHandler : IBillingHandler
{
    private readonly ILogger<BillingHandler> _logger;

    public BillingHandler(ILogger<BillingHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "BillingHandler: generating invoice for SubscriptionId={SubscriptionId}, CustomerId={CustomerId}, Plan={Plan}",
            ev.SubscriptionId,
            ev.CustomerId,
            ev.PlanCode);

        return Task.CompletedTask;
    }
}
