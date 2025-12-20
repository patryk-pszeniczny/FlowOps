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
        private readonly ITimeProvider _clock;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SuspendSubscriptionCommandHandler> _logger;

        public SuspendSubscriptionCommandHandler(
            ISubscriptionRepository repository,
            ITimeProvider clock,
            IUnitOfWork unitOfWork,
            ILogger<SuspendSubscriptionCommandHandler> logger)
        {
            _repository = repository;
            _clock = clock;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task HandleAsync(SuspendSubscriptionCommand command, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(command.SubscriptionId, asNoTracking: false, ct: ct)
                ?? throw new KeyNotFoundException($"Subscription with ID '{command.SubscriptionId}' not found.");

            subscription.Suspend(_clock.UtcNow);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Subscription {SubscriptionId} suspended.", command.SubscriptionId);
        }
    }
}
