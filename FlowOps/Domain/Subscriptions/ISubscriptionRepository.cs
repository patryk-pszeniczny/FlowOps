using System.Diagnostics.CodeAnalysis;

namespace FlowOps.Domain.Subscriptions
{
    public interface ISubscriptionRepository
    {
        Task AddAsync(Subscription subscription, CancellationToken ct = default);

        Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
