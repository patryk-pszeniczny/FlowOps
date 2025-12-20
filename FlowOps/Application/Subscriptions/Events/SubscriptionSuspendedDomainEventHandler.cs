using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Events
{
    public sealed class SubscriptionSuspendedDomainEventHandler : IDomainEventHandler<SubscriptionSuspendedDomainEvent>
    {
        private readonly IOutboxMessageWriter _outbox;

        public SubscriptionSuspendedDomainEventHandler(IOutboxMessageWriter outbox)
        {
            _outbox = outbox;
        }

        public async Task HandleAsync(SubscriptionSuspendedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new SubscriptionSuspendedEvent
            {
                SubscriptionId = domainEvent.SubscriptionId,
                CustomerId = domainEvent.CustomerId,
                PlanCode = domainEvent.PlanCode
            };

            await _outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}