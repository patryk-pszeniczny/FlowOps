namespace FlowOps.BuildingBlocks.Integration
{
    public interface IIntegrationEventInbox
    {
        Task<bool> HasProcessedAsync(string consumer, Guid eventId, CancellationToken cancellationToken = default);
        Task MarkProcessedAsync(string consumer, Guid eventId, CancellationToken cancellationToken = default);
        Task ProcessAsync<TEvent>(string consumer, TEvent evt, Func<CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IntegrationEvent;
    }
}
