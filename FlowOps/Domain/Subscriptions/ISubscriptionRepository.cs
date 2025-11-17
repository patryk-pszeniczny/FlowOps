using System.Diagnostics.CodeAnalysis;

namespace FlowOps.Domain.Subscriptions
{
    public interface ISubscriptionRepository
    {
        void Add(Subscription subscription);

        bool TryGet(Guid id, [MaybeNullWhen(false)] out Subscription? subscription);
    }
}
