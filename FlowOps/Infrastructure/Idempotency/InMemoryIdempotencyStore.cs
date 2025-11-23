
using System.Collections.Concurrent;

namespace FlowOps.Infrastructure.Idempotency
{
    public sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly ConcurrentDictionary<string, Guid> _store = new(StringComparer.OrdinalIgnoreCase);
        public bool TryGet(string key, out Guid subscriptionId)
        {
            if(string.IsNullOrWhiteSpace(key))
            {
                subscriptionId = Guid.Empty;
                return false;
            }
            return _store.TryGetValue(key.Trim(), out subscriptionId);

        }

        public void Set(string key, Guid subscriptionId)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            _store.TryAdd(key.Trim(), subscriptionId);
        }
    }
}
