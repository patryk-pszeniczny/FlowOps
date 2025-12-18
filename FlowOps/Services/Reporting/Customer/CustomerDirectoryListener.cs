using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Services.Reporting.Customer
{
    public sealed class CustomerDirectoryListener : BackgroundService
    {
        private readonly IEventBus _bus;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CustomerDirectoryListener> _logger;

        public CustomerDirectoryListener(
            IEventBus bus,
            IServiceScopeFactory scopeFactory,
            ILogger<CustomerDirectoryListener> logger)
        {
            _bus = bus;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _bus.Subscribe<CustomerCreatedEvent>(evt => OnCustomerCreatedAsync(evt, stoppingToken));

            _logger.LogInformation("CustomerDirectoryListener subscribed to CustomerCreatedEvent.");
            return Task.CompletedTask;
        }

        private async Task OnCustomerCreatedAsync(CustomerCreatedEvent evt, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            if (evt is null)
                throw new ArgumentNullException(nameof(evt));

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<CustomerCreatedEvent>>();

            try
            {
                await handler.HandleAsync(evt, ct).ConfigureAwait(false);

                _logger.LogInformation(
                    "CustomerDirectoryListener handled CustomerCreatedEvent (EventId={EventId}, CustomerId={CustomerId}).",
                    evt.Id,
                    evt.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CustomerDirectoryListener failed handling CustomerCreatedEvent (EventId={EventId}, CustomerId={CustomerId}).",
                    evt.Id,
                    evt.CustomerId);

                throw;
            }
        }
    }
}
