
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Infrastructure.Sql;
using FlowOps.Infrastructure.Sql.Reporting.Customer;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Services.Reporting.Customer
{
    public sealed class CustomerDirectoryProjector : IHostedService
    {
        private readonly IEventBus _bus;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CustomerDirectoryProjector> _logger;

        public CustomerDirectoryProjector(
            IEventBus bus,
            IServiceScopeFactory scopeFactory,
            ILogger<CustomerDirectoryProjector> logger)
        {
            _bus = bus;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _bus.Subscribe<CustomerCreatedEvent>(HandleAsync);

            _logger.LogInformation("CustomerDirectoryProjector subscribed to CustomerCreatedEvent.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task HandleAsync(CustomerCreatedEvent e)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();

                var set = db.Set<CustomerDirectoryEntry>();

                var existing = await set.FirstOrDefaultAsync(x => x.CustomerId == e.CustomerId);

                if (existing is null)
                {
                    existing = new CustomerDirectoryEntry
                    {
                        CustomerId = e.CustomerId
                    };
                    set.Add(existing);
                }

                existing.Name = e.Name;
                existing.TaxId = e.TaxId;
                existing.Email = e.Email;
                existing.CreatedAt = e.CreatedAt;

                await db.SaveChangesAsync();

                _logger.LogInformation(
                    "CustomerDirectory upserted: CustomerId={CustomerId}, Name={Name}",
                    e.CustomerId,
                    e.Name);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "CustomerDirectoryProjector DB update failed for CustomerId={CustomerId} (TaxId/Email uniqueness?)",
                    e.CustomerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CustomerDirectoryProjector failed for CustomerId={CustomerId}",
                    e.CustomerId);
                throw;
            }
        }
    }
}
