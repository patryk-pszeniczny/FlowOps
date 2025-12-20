using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Events
{
    public sealed class SubscriptionCancelledDomainEventHandler : IDomainEventHandler<SubscriptionCancelledDomainEvent>
    {
        private readonly IOutboxMessageWriter _outbox;

        public SubscriptionCancelledDomainEventHandler(IOutboxMessageWriter outbox)
        {
            _outbox = outbox;
        }

        public async Task HandleAsync(SubscriptionCancelledDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new SubscriptionCancelledEvent
            {
                SubscriptionId = domainEvent.SubscriptionId,
                CustomerId = domainEvent.CustomerId,
                PlanCode = domainEvent.PlanCode
            };

            await _outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}