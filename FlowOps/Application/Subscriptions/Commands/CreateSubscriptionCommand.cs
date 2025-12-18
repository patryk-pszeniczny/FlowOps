using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Plans;
using FlowOps.Domain.Subscriptions;

namespace FlowOps.Application.Subscriptions.Commands
{
    public sealed record CreateSubscriptionCommand(Guid CustomerId, string PlanCode);
    public sealed class CreateSubscriptionCommandHandler
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly IPlanPricing _pricing;
        private readonly ITimeProvider _clock;

        public CreateSubscriptionCommandHandler(
            ISubscriptionRepository repository,
            IEventBus eventBus,
            IPlanPricing pricing,
            ITimeProvider clock)
        {
            _repository = repository;
            _eventBus = eventBus;
            _pricing = pricing;
            _clock = clock;
        }
        public async Task<Guid> HandleAsync(CreateSubscriptionCommand command, CancellationToken ct = default)
        {
            _ = _pricing.GetPrice(command.PlanCode);

            var subscription = Subscription.Create(
                command.CustomerId,
                command.PlanCode);
            var activaed = subscription.Activate(_clock.UtcNow);

            await _repository.AddAsync(subscription, ct);
            await _eventBus.PublishAsync(activaed);

            return subscription.Id;
        }
    }
}
