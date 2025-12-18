using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Application.Customers.Commands
{
    public sealed record CreateCustomerCommand(string name, string? TaxId, string? Email);
    public sealed class CreateCustomerCommandHandler
    {
        private readonly ICustomerRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly ITimeProvider _clock;
        private readonly ILogger<CreateCustomerCommandHandler> _logger; 
        public CreateCustomerCommandHandler(
            ICustomerRepository repository,
            IEventBus eventBus,
            ITimeProvider clock,
            ILogger<CreateCustomerCommandHandler> logger)
        {
            _repository = repository;
            _eventBus = eventBus;
            _clock = clock;
            _logger = logger;
        }
        public async Task<Customer> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
        {
            var customer = Customer.Create(command.name, command.TaxId, command.Email, _timeProvider.UtcNow);
            if (!string.IsNullOrWhiteSpace(customer.TaxId))
            {
                if (await _repository.ExistsByTaxIdAsync(customer.TaxId!, ct))
                {
                    throw new InvalidOperationException($"A customer with Tax ID '{customer.TaxId}' already exists.");
                }
            }

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                if (await _repository.ExistsByEmailAsync(customer.Email!, ct))
                {
                    throw new InvalidOperationException($"A customer with Email '{customer.Email}' already exists.");
                }
            }

            await _repository.AddAsync(customer, ct);

            var @event = new CustomerCreatedEvent
            {
                CustomerId = customer.Id,
                Name = customer.Name,
                TaxId = customer.TaxId,
                Email = customer.Email,
                CreatedAt = customer.CreatedAt
            };

            await _eventBus.PublishAsync(@event);

            _logger.LogInformation(
                "Created customer CustomerId={CustomerId}, Name={Name} and published CustomerCreatedEvent.",
                customer.Id,
                customer.Name);

            return customer;
        }

    }
}
