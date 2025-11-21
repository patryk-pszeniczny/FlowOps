
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Services.Reporting
{
    public class ReportingListener : IHostedService
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<ReportingListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        public ReportingListener(
            IEventBus eventBus, 
            ILogger<ReportingListener> logger,
            IServiceScopeFactory scopeFactory)
        {
            _eventBus = eventBus;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventBus.Subscribe<SubscriptionActivatedEvent>(HandleActivation);
            _eventBus.Subscribe<InvoiceIssuedEvent>(HandleInvoice);
            _eventBus.Subscribe<InvoicePaidEvent>(HandlePaid);
            _eventBus.Subscribe<SubscriptionCancelledEvent>(HandleCancelled);
            _eventBus.Subscribe<SubscriptionResumedEvent>(HandleResumed);
            _eventBus.Subscribe<SubscriptionSuspendedEvent>(HandleSuspended);

            _logger.LogInformation("ReportingListener subscribed to SubscriptionActivatedEvent");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ReportingListener stopping");
            return Task.CompletedTask;
        }
        private Task HandleActivation(SubscriptionActivatedEvent ev) => WithHandler(h => h.On(ev));
        private Task HandleInvoice(InvoiceIssuedEvent ev) => WithHandler(h => h.On(ev));
        private Task HandlePaid(InvoicePaidEvent ev) => WithHandler(h => h.On(ev));
        private Task HandleCancelled(SubscriptionCancelledEvent ev) => WithHandler(h => h.On(ev));
        private Task HandleResumed(SubscriptionResumedEvent ev) => WithHandler(h => h.On(ev));
        private Task HandleSuspended(SubscriptionSuspendedEvent ev) => WithHandler(h => h.On(ev));

        private async Task WithHandler(Func<IReportingHandler, Task> action)
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IReportingHandler>();
            await action(handler);
        }
    }
}
