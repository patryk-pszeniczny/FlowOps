using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.BuildingBlocks.Messaging
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
