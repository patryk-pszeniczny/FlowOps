using FlowOps.BuildingBlocks.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowOps.BuildingBlocks.Messaging
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Func<InegrationEvent, Task>>> _handlers = new();
        public Task PublishAsync<T>(T @event) where T : InegrationEvent
        {
            var eventType = typeof(T);
            if(_handlers.TryGetValue(eventType, out var handlers))
            {
                var tasks = handlers.Select(handler => handler(@event));
                return Task.WhenAll(tasks);
            }
            return Task.CompletedTask;
        }

        public void Subscribe<T>(Func<T, Task> handler) where T : InegrationEvent
        {
            var eventType = typeof(T);
            if(!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Func<InegrationEvent, Task>>();
                _handlers.Add(eventType, handlers);
            }
            handlers.Add(evt => handler((T)evt));
        }
    }
}
