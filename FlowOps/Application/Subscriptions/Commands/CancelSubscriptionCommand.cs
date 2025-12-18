using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Commands
{
    public sealed record CancelSubscriptionCommand(Guid SubscriptionId);
    
    public sealed class CancelSubscriptionCommandHandler
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _clock;
        private readonly IUnitOfWork _unitOfWork;

        public CancelSubscriptionCommandHandler(
            ISubscriptionRepository repository,
            IEventBus eventBus,
            ITimeProvider clock,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _eventBus = eventBus;
            _clock = clock;
            _unitOfWork = unitOfWork;
        }
        public async Task HandleAsync(CancelSubscriptionCommand command, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(command.SubscriptionId, asNoTracking: false, ct: ct)
                ?? throw new KeyNotFoundException($"Subscription with ID '{command.SubscriptionId}' not found.");
            subscription.Cancel(_clock.UtcNow);

            await _unitOfWork.SaveChangesAsync(ct);
            await _eventBus.PublishAsync(new SubscriptionCancelledEvent
            {
                SubscriptionId = subscription.Id,
                CustomerId = subscription.CustomerId,
                PlanCode = subscription.PlanCode
            });
        }
    }

}
