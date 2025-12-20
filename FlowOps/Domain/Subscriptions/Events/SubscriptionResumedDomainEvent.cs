using FlowOps.Domain.Events;

namespace FlowOps.Domain.Subscriptions.Events
{
    public sealed record SubscriptionResumedDomainEvent(
        Guid SubscriptionId,
        Guid CustomerId,
        string PlanCode) : DomainEventBase;
}
