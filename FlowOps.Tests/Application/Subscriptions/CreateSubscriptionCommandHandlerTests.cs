using FlowOps.Application.Common;
using FlowOps.Application.Subscriptions.Commands;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Plans;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Subscriptions;
using FluentAssertions;
using Xunit;

namespace FlowOps.Tests.Application.Subscriptions;

public class CreateSubscriptionCommandHandlerTests
{
    [Fact]
    public async Task Creates_subscription_and_publishes_event()
    {
        var repository = new InMemorySubscriptionRepository();
        var eventBus = new FakeEventBus();
        var pricing = new FakePlanPricing();
        var clock = new StubTimeProvider(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new CreateSubscriptionCommandHandler(repository, eventBus, pricing, clock);

        var subscriptionId = await handler.HandleAsync(new CreateSubscriptionCommand(Guid.NewGuid(), "BASIC"));

        var stored = await repository.GetByIdAsync(subscriptionId);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(SubscriptionStatus.Active);
        eventBus.Published.Should().ContainSingle(e => e is IntegrationEvent ev && ev is not null && ev.Id != Guid.Empty);
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

    private sealed class FakePlanPricing : IPlanPricing
    {
        public IReadOnlyDictionary<string, decimal> GetAll() => new Dictionary<string, decimal> { { "BASIC", 10m } };

        public decimal GetPrice(string planCode)
        {
            if (!GetAll().ContainsKey(planCode))
            {
                throw new ArgumentException("Unknown plan.", nameof(planCode));
            }
            return 10m;
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