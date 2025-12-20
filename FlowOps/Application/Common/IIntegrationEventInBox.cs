using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.Application.Common
{
    public interface IIntegrationEventInBox
    {
        Task<bool> HasProcessedAsync(string consumer, Guid eventId, CancellationToken cancellationToken = default);
        Task MarkProcessedAsync(string consumer, Guid eventId, CancellationToken cancellationToken = default);
        Task ProcessAsync<TEvent>(string conusmer, TEvent evt, Func<CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IntegrationEvent;
    }
}
