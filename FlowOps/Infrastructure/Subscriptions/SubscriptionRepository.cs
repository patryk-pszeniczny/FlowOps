using FlowOps.Domain.Subscriptions;
using System.Collections.Concurrent;

namespace FlowOps.Infrastructure.Subscriptions
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ConcurrentDictionary<Guid, Subscription> _store = new();

        public Task<Subscription?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken ct = default)
        {
            _store.TryGetValue(id, out var subscription);
            return Task.FromResult(subscription);
        }

        public Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        {
            var items = _store.Values
                .Where(s => s.CustomerId == customerId)
                .ToList()
                .AsReadOnly();
            return Task.FromResult((IReadOnlyList<Subscription>)items);
        }

        public Task AddAsync(Subscription subscription, CancellationToken ct = default)
        {
            if (!_store.TryAdd(subscription.Id, subscription))
            {
                throw new InvalidOperationException($"Subscription with ID {subscription.Id} already exists.");
            }
            return Task.CompletedTask;
        }
    }
}
