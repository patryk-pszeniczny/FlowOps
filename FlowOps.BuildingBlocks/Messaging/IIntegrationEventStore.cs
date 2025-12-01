using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.BuildingBlocks.Messaging
{
    public interface IIntegrationEventStore
    {
        Task AppendAsync(IntegrationEvent @event, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IntegrationEvent>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
