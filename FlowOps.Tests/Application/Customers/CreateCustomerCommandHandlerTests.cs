using FlowOps.Application.Common;
using FlowOps.Application.Customers.Commands;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Customers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace FlowOps.Tests.Application.Customers;

public class CreateCustomerCommandHandlerTests
{
    [Fact]
    public async Task Creates_customer_and_publishes_event()
    {
        var repository = new InMemoryCustomerRepository();
        var eventBus = new FakeEventBus();
        var handler = new CreateCustomerCommandHandler(repository, eventBus, new StubTimeProvider(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)), NullLogger<CreateCustomerCommandHandler>.Instance);

        var customer = await handler.HandleAsync(new CreateCustomerCommand("Acme", "PL123", "test@example.com"));

        customer.Should().NotBeNull();
        repository.Stored.Should().ContainSingle(c => c.Id == customer.Id);
        eventBus.Published.Should().HaveCount(1);
    }

    [Fact]
    public async Task Throws_on_duplicate_email()
    {
        var repository = new InMemoryCustomerRepository();
        var handler = new CreateCustomerCommandHandler(repository, new FakeEventBus(), new StubTimeProvider(DateTime.UtcNow), NullLogger<CreateCustomerCommandHandler>.Instance);

        await handler.HandleAsync(new CreateCustomerCommand("Acme", null, "dup@example.com"));

        Func<Task> act = async () => await handler.HandleAsync(new CreateCustomerCommand("Other", null, "dup@example.com"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class FakeEventBus : IEventBus
    {
        public List<IntegrationEvent> Published { get; } = new();

        public Task PublishAsync<T>(T @event) where T : IntegrationEvent
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent
        {
        }
    }

    private sealed class InMemoryCustomerRepository : ICustomerRepository
    {
        public List<Customer> Stored { get; } = new();

        public Task AddAsync(Customer customer, CancellationToken ct = default)
        {
            if (Stored.Any(c => c.Id == customer.Id))
            {
                throw new InvalidOperationException("Customer already exists.");
            }
            Stored.Add(customer);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        {
            return Task.FromResult(Stored.Any(c => string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct = default)
        {
            return Task.FromResult(Stored.Any(c => string.Equals(c.TaxId, taxId, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Stored.FirstOrDefault(c => c.Id == id));
        }

        public Task<IReadOnlyList<Customer>> ListAsync(int take, CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<Customer>>(Stored.Take(Math.Clamp(take, 1, 200)).ToList());
        }
    }

    private sealed class StubTimeProvider : ITimeProvider
    {
        public StubTimeProvider(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}