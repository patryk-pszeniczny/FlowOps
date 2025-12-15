using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Contracts.Request.Customers;
using FlowOps.Events;
using FlowOps.Infrastructure.Customer;
using FlowOps.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Application.Customer
{
    public sealed class CustomerCommandService
    {
        private readonly FlowOpsDbContext _database;
        private readonly IEventBus _eventsBus;
        private readonly ILogger<CustomerCommandService> _logger;

        public CustomerCommandService(
            FlowOpsDbContext database,
            IEventBus eventsBus,
            ILogger<CustomerCommandService> logger)
        {
            _database = database;
            _eventsBus = eventsBus;
            _logger = logger;
        }
        public async Task<CustomerEntity> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
        {
            if(request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Customer name is required.", nameof(request.Name));
            }
            var taxId = string.IsNullOrWhiteSpace(request.TaxId) ? null : request.TaxId.Trim();
            var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

            if(taxId is not null)
            {
                var existsTax = await _database.Customers
                    .AsNoTracking()
                    .AnyAsync(c => c.TaxId == taxId, ct);
                if (existsTax)
                {
                    throw new InvalidOperationException($"A customer with Tax ID '{taxId}' already exists.");
                }
            }
            if(email is not null)
            {
                var existsEmail = await _database.Customers
                    .AsNoTracking()
                    .AnyAsync(c => c.Email == email, ct);
                if (existsEmail)
                {
                    throw new InvalidOperationException($"A customer with Email '{email}' already exists.");
                }
            }
            var entity = new CustomerEntity
            {
                CustomerId = Guid.NewGuid(),
                Name = name,
                TaxId = taxId,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            _database.Customers.Add(entity);
            try
            {
                await _database.SaveChangesAsync(ct);
            }
            catch(DbUpdateException dbEx)
            {
                _logger.LogWarning(dbEx, "Customer create failed (DB constraint). TaxId={TaxId}, Email={Email}", taxId, email);
                throw new InvalidOperationException("Customer create failed due to database constraint (TaxId/Email must be unique).");
            }
            var @event = new CustomerCreatedEvent
            {
                CustomerId = entity.CustomerId,
                Name = entity.Name,
                TaxId = entity.TaxId,
                Email = entity.Email,
                CreatedAt = entity.CreatedAt
            };

            await _eventsBus.PublishAsync(@event);

            _logger.LogInformation(
                "Created customer CustomerId={CustomerId}, Name={Name} and published CustomerCreatedEvent (EventId={EventId}).",
                entity.CustomerId,
                entity.Name,
                @event.Id);

            return entity;
        }
    }
}
