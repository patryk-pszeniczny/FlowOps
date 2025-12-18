using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Events;

namespace FlowOps.Application.Subscriptions.Commands
{
    public sealed record ResumeSubscriptionCommand(Guid SubscriptionId);
    public sealed class ResumeSubscriptionCommandHandler
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _clock;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ResumeSubscriptionCommandHandler> _logger;

        public ResumeSubscriptionCommandHandler(
            ISubscriptionRepository repository,
            IEventBus eventBus,
            ITimeProvider clock,
            IUnitOfWork unitOfWork,
            ILogger<ResumeSubscriptionCommandHandler> logger)
        {
            _repository = repository;
            _eventBus = eventBus;
            _clock = clock;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task HandleAsync(ResumeSubscriptionCommand command, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(command.SubscriptionId, asNoTracking: false, ct: ct)
                         ?? throw new KeyNotFoundException($"Subscription with ID '{command.SubscriptionId}' not found.");

            subscription.Resume(_clock.UtcNow);

            await _unitOfWork.SaveChangesAsync(ct);

            await _eventBus.PublishAsync(new SubscriptionResumedEvent
            {
                SubscriptionId = subscription.Id,
                CustomerId = subscription.CustomerId,
                PlanCode = subscription.PlanCode
            });
            _logger.LogInformation("Subscription {SubscriptionId} resumed.", command.SubscriptionId);
        }
    }
}
