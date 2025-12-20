using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Customers.Events;
using FlowOps.Events;

namespace FlowOps.Application.Customers.Events
{
    public sealed class CustomerCreatedDomainEventHandler : IDomainEventHandler<CustomerCreatedDomainEvent>
    {
        private readonly IOutboxMessageWriter _outbox;
        public CustomerCreatedDomainEventHandler(IOutboxMessageWriter outbox)
        {
            _outbox = outbox;
        }
        public async Task HandleAsync(CustomerCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            var integrationEvent = new CustomerCreatedEvent
            {
                CustomerId = domainEvent.CustomerId,
                Name = domainEvent.Name,
                TaxId = domainEvent.TaxId,
                Email = domainEvent.Email,
                CreatedAt = domainEvent.CreatedAt
            };
            await _outbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
