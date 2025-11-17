using System.Collections.Concurrent;

namespace FlowOps.Domain.Subscriptions
{
    public class InMemorySubscriptionRepository : ISubscriptionRepository
    {
        private readonly ConcurrentDictionary<Guid, Subscription> _store = new();
        
        public void Add(Subscription subscription)
        {
            if(!_store.TryAdd(subscription.Id, subscription))
            {
                throw new InvalidOperationException($"Subscription with ID {subscription.Id} already exists.");
            }
        }
        public bool TryGet(Guid id, out Subscription? subscription)
            => _store.TryGetValue(id, out subscription);
    }
}
