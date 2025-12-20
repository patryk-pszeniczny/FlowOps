using FlowOps.Domain.Events;

namespace FlowOps.Domain.Customers.Events
{
    public sealed record CustomerCreatedDomainEvent(
        Guid CustomerId,
        string Name,
        string? TaxId,
        string? Email,
        DateTime CreatedAt) : DomainEventBase;
}
