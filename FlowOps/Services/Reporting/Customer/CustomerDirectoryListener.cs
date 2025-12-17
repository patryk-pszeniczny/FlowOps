using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Services.Reporting.Customer
{
    public sealed class CustomerDirectoryListener : BackgroundService
    {
        private readonly IEventBus _bus;
        private readonly IIntegrationEventHandler<CustomerCreatedEvent> _handler;
        private readonly ILogger<CustomerDirectoryListener> _logger;

        public CustomerDirectoryListener(
            IEventBus bus,
            IIntegrationEventHandler<CustomerCreatedEvent> handler,
            ILogger<CustomerDirectoryListener> logger)
        {
            _bus = bus;
            _handler = handler;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _bus.Subscribe<CustomerCreatedEvent>(async (ev) =>
            {
                await _handler.HandleAsync(ev, stoppingToken).ConfigureAwait(false);
            });
            
            _logger.LogInformation("CustomerDirectoryListener subscribed to CustomerCreatedEvent.");
            return Task.CompletedTask;
        }
    }
}
