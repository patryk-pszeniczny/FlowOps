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
        private readonly IPlanPricing _pricing;
        private readonly ITimeProvider _clock;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

        public CreateSubscriptionCommandHandler(
            ISubscriptionRepository repository,
            IPlanPricing pricing,
            ITimeProvider clock,
            IUnitOfWork unitOfWork,
            ILogger<CreateSubscriptionCommandHandler> logger)
        {
            _repository = repository;
            _pricing = pricing;
            _clock = clock;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Guid> HandleAsync(CreateSubscriptionCommand command, CancellationToken ct = default)
        {
            _ = _pricing.GetPrice(command.PlanCode);

            var subscription = Subscription.Create(
                command.CustomerId,
                command.PlanCode);

            subscription.Activate(_clock.UtcNow);

            await _repository.AddAsync(subscription, ct);
            await _unitOfWork.SaveChangesAsync(ct);


            _logger.LogInformation("Subscription {SubscriptionId} created and activated for customer {CustomerId} on plan {PlanCode}.",
                subscription.Id, command.CustomerId, command.PlanCode);
            return subscription.Id;
        }
    }
}
