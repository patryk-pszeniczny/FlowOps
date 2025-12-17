using FlowOps.BuildingBlocks.Integration;
using FlowOps.Events;
using FlowOps.Infrastructure.Sql;
using FlowOps.Infrastructure.Sql.Reporting.Customer;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Services.Reporting.Customer
{
    public sealed class CustomerCreatedEventHandler : IIntegrationEventHandler<CustomerCreatedEvent>
    {
        private readonly FlowOpsDbContext _database;
        private readonly ILogger<CustomerCreatedEventHandler> _logger;
        public CustomerCreatedEventHandler(
            FlowOpsDbContext database,
            ILogger<CustomerCreatedEventHandler> logger)
        {
            _database = database;
            _logger = logger;
        }
        public async Task HandleAsync(CustomerCreatedEvent @event, CancellationToken cancellationToken)
        {
            var set = _database.Set<CustomerDirectoryEntry>();

            var entry = await set
                .FirstOrDefaultAsync(x => x.CustomerId == @event.CustomerId, cancellationToken)
                .ConfigureAwait(false);

            if(entry is null)
            {
                entry = new CustomerDirectoryEntry
                {
                    CustomerId = @event.CustomerId,
                    Name = @event.Name,
                    TaxId = @event.TaxId,
                    Email = @event.Email,
                    CreatedAt = @event.CreatedAt
                };

                set.Add(entry);

                _logger.LogInformation(
                    "CustomerDirectory: inserted CustomerId={CustomerId}, Name={Name}, Tax={TaxId}, Email={Email}.",
                    @event.CustomerId,
                    @event.Name,
                    @event.TaxId,
                    @event.Email);
            }
            else
            {
                entry.Name = @event.Name;
                entry.TaxId = @event.TaxId;
                entry.Email = @event.Email;

                if(entry.CreatedAt == default && @event.CreatedAt != default)
                {
                    entry.CreatedAt = @event.CreatedAt;
                }

                _logger.LogInformation(
                    "CustomerDirectory: updated CustomerId={CustomerId}, Name={Name}, Tax={TaxId}, Email={Email}.",
                    @event.CustomerId,
                    @event.Name,
                    @event.TaxId,
                    @event.Email);
            }
            await _database.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
