using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions
{
    public class SubscriptionCommandService
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IEventBus _eventBus;

        public SubscriptionCommandService(ISubscriptionRepository repository, IEventBus eventBus)
        {
            _repository = repository;
            _eventBus = eventBus;
        }
        public async Task<Guid> CreateAsync(Guid customerId, string planCode, DateTime utcNow, CancellationToken ct = default)
        {
            var subscription = Subscription.Create(customerId, planCode);
            var ev = subscription.Activate(utcNow);

            _repository.Add(subscription);
            await _eventBus.PublishAsync(ev);

            return subscription.Id;
        }
        public async Task CancelAsync(Guid subscriptionId, DateTime utcNow, CancellationToken ct = default)
        {
            if(!_repository.TryGet(subscriptionId, out var subscription) || subscription is null)
            {
                throw new KeyNotFoundException($"Subscription {subscriptionId} not found.");
            }
            subscription.Cancel(utcNow);

            var ev = new SubscriptionCancelledEvent
            {
                SubscriptionId = subscriptionId,
                CustomerId = subscription.CustomerId,
                PlanCode = subscription.PlanCode,
            };
            await _eventBus.PublishAsync(ev);
        }
    }
}
