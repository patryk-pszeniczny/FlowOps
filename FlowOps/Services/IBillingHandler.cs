using FlowOps.Events;

namespace FlowOps.Services
{
    public interface IBillingHandler
    {
        Task HandleAsync(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default);
    }
}
