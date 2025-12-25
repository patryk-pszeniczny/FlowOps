using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.BuildingBlocks.Messaging
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
