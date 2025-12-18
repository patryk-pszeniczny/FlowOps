using System.Diagnostics.CodeAnalysis;

namespace FlowOps.Domain.Subscriptions
{
    public interface ISubscriptionRepository
    {
        Task AddAsync(Subscription subscription, CancellationToken ct = default);

        Task<Subscription?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken ct = default);
        Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    }
}
