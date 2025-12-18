using FlowOps.Domain.Subscriptions;
using System.Collections.Concurrent;

namespace FlowOps.Infrastructure.Subscriptions
{
    public class InMemorySubscriptionRepository : ISubscriptionRepository
    {
        private readonly ConcurrentDictionary<Guid, Subscription> _store = new();
        
        public Task AddAsync(Subscription subscription, CancellationToken ct = default)
        {
            if(!_store.TryAdd(subscription.Id, subscription))
            {
                throw new InvalidOperationException($"Subscription with ID {subscription.Id} already exists.");
            }
            return Task.CompletedTask;
        }
        public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            _store.TryGetValue(id, out var subscription);
            return Task.FromResult(subscription);
        }
    }
}
