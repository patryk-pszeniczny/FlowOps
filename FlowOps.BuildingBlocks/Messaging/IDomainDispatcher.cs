using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.BuildingBlocks.Messaging
{
    public interface IDomainDispatcher
    {
        Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
