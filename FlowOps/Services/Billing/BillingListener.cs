using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Services.Billing
{
    public class BillingListener : IHostedService
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<BillingListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private const string ConsumerName = "Billing";
        public BillingListener(
            IEventBus eventBus, 
            ILogger<BillingListener> logger,
            IServiceScopeFactory scopeFactory)
        {
            _eventBus = eventBus;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventBus.Subscribe<SubscriptionActivatedEvent>(Handle);
            _logger.LogInformation("BillingListener subscribed to SubscriptionActivatedEvent");

            _eventBus.Subscribe<SubscriptionCancelledEvent>(HandleCancelled);
            _logger.LogInformation("BillingListener subscribed to SubscriptionCancelledEvent");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BillingListener stopping");
            return Task.CompletedTask;
        }
        private async Task Handle(SubscriptionActivatedEvent ev)
        {
            using var scope = _scopeFactory.CreateScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventInbox>();
            var handler = scope.ServiceProvider.GetRequiredService<IBillingHandler>();

            await inbox.ProcessAsync(ConsumerName, ev, token => handler.HandleAsync(ev), 
                CancellationToken.None)
                .ConfigureAwait(false);
        }
        private Task HandleCancelled(SubscriptionCancelledEvent ev)
        {
            _logger.LogInformation(
                "Billing: subscription {SubscriptionId} cancelled for Customer {CustomerId}, Plan {PlanCode}",
                ev.SubscriptionId, ev.CustomerId, ev.PlanCode);
            return Task.CompletedTask;
        }
    }
}
