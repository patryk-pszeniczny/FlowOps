using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Commands
{
    public sealed record SuspendSubscriptionCommand(Guid SubscriptionId);
    public sealed class SuspendSubscriptionCommandHandler
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _clock;
        private readonly ILogger<SuspendSubscriptionCommandHandler> _logger;

        public SuspendSubscriptionCommandHandler(
            ISubscriptionRepository repository,
            IEventBus eventBus,
            ITimeProvider clock,
            ILogger<SuspendSubscriptionCommandHandler> logger)
        {
            _repository = repository;
            _eventBus = eventBus;
            _clock = clock;
            _logger = logger;
        }
        public async Task HandleAsync(SuspendSubscriptionCommand command, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(command.SubscriptionId, ct)
                ?? throw new KeyNotFoundException($"Subscription with ID '{command.SubscriptionId}' not found.");

            subscription.Suspend(_clock.UtcNow);

            await _eventBus.PublishAsync(new SubscriptionSuspendedEvent
            {
                SubscriptionId = subscription.Id,
                CustomerId = subscription.CustomerId,
                PlanCode = subscription.PlanCode
            });
            _logger.LogInformation("Subscription {SubscriptionId} suspended.", command.SubscriptionId);
        }
    }
}
