using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Events
{
    public sealed class SubscriptionResumedDomainEventHandler : IDomainEventHandler<SubscriptionResumedDomainEvent>
    {
        private readonly IOutboxMessageWriter _outbox;

        public SubscriptionResumedDomainEventHandler(IOutboxMessageWriter outbox)
        {
            _outbox = outbox;
        }

        public async Task HandleAsync(SubscriptionResumedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new SubscriptionResumedEvent
            {
                SubscriptionId = domainEvent.SubscriptionId,
                CustomerId = domainEvent.CustomerId,
                PlanCode = domainEvent.PlanCode
            };

            await _outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}