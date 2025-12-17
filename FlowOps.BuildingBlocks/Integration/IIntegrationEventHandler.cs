namespace FlowOps.BuildingBlocks.Integration
{
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    }
}
