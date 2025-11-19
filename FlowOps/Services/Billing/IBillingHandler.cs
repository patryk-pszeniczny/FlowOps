using FlowOps.Events;

namespace FlowOps.Services.Billing
{
    public interface IBillingHandler
    {
        Task HandleAsync(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default);
    }
}
