using FlowOps.BuildingBlocks.Integration;
using System.Collections.Concurrent;

namespace FlowOps.Services.Replay
{
    public sealed class EventRecorder
    {
        private readonly ConcurrentQueue<IntegrationEvent> _events = new();

        public void Append(IntegrationEvent ev) => _events.Enqueue(ev);

        public IReadOnlyList<IntegrationEvent> Snapshot()
        {
            return _events.ToArray();
        }
        public void Clear()
        {
            while (_events.TryDequeue(out _)) { }
        }
    }
}
