using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Events
{
    public sealed class SubscriptionActivatedDomainEventHandler : IDomainEventHandler<SubscriptionActivatedDomainEvent>
    {
        private readonly IOutboxMessageWriter _outbox;

        public SubscriptionActivatedDomainEventHandler(IOutboxMessageWriter outbox)
        {
            _outbox = outbox;
        }

        public async Task HandleAsync(SubscriptionActivatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new SubscriptionActivatedEvent
            {
                SubscriptionId = domainEvent.SubscriptionId,
                CustomerId = domainEvent.CustomerId,
                PlanCode = domainEvent.PlanCode
            };

            await _outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}