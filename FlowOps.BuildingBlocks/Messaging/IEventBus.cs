using FlowOps.BuildingBlocks.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowOps.BuildingBlocks.Messaging
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event) where T : InegrationEvent;
        void Subscribe<T>(Func<T, Task> handler) where T : InegrationEvent;
    }
}
