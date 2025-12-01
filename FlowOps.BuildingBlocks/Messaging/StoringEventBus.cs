using FlowOps.BuildingBlocks.Integration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowOps.BuildingBlocks.Messaging
{
    public sealed class StoringEventBus : IEventBus
    {
        private readonly IEventBus _inner;
        private readonly IIntegrationEventStore _store;
        private readonly ILogger<StoringEventBus> _logger;
        public StoringEventBus(
            IEventBus inner, 
            IIntegrationEventStore store, 
            ILogger<StoringEventBus> logger)
        {
            _inner = inner;
            _store = store;
            _logger = logger;
        }
        public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
        {
            if(@event is null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            try
            {
                await _store.AppendAsync(@event);
                _logger.LogDebug(
                    "Integration event {EventType} with Id={EventId} stored to event store. Forwarding to inner bus...",
                    @event.GetType().FullName,
                    @event.Id);

                await _inner.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while publishing integration event {EventType} with Id={EventId}",
                    @event.GetType().FullName,
                    @event.Id);
                throw;
            }
        }

        public void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent
        {
             _inner.Subscribe(handler);
        }
    }
}
