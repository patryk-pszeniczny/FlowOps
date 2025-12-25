using System.Reflection;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.Domain.Subscriptions;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Inbox;
using FlowOps.Infrastructure.Persistence.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using FlowOps.BuildingBlocks.Domain.Events;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Application.Common;

namespace FlowOps.Tests.Infrastructure.Persistence
{
    public class FlowOpsDbContextTests
    {
        [Fact]
        public async Task SaveChanges_dispatches_domain_events_and_saves_entities()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var dispatched = new List<IDomainEvent>();
            using var context = CreateContext(connection, new RecordingDispatcher(dispatched));
            await context.Database.EnsureCreatedAsync();

            var subscription = Subscription.Create(Guid.NewGuid(), "basic");
            subscription.Activate(DateTime.UtcNow);

            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();

            dispatched.Should().ContainSingle(e => e is SubscriptionActivatedDomainEvent);
            (await context.Subscriptions.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task Outbox_messages_are_published_and_marked_processed()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddSingleton<IDomainEventDispatcher, NoOpDispatcher>();
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            services.AddLogging();
            services.AddDbContext<FlowOpsDbContext>(opt => opt.UseSqlServer(connection));
            services.AddScoped<IOutboxMessageWriter, OutboxMessageWriter>();
            services.AddSingleton<OutboxMessageProcessor>();

            var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();
                await db.Database.EnsureCreatedAsync();
            }

            var bus = (InMemoryEventBus)provider.GetRequiredService<IEventBus>();
            var published = new List<IntegrationEvent>();
            bus.Subscribe<SubscriptionActivatedEvent>(evt =>
            {
                published.Add(evt);
                return Task.CompletedTask;
            });

            using (var scope = provider.CreateScope())
            {
                var writer = scope.ServiceProvider.GetRequiredService<IOutboxMessageWriter>();
                var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();

                var integrationEvent = new SubscriptionActivatedEvent
                {
                    SubscriptionId = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    PlanCode = "BASIC"
                };

                await writer.AddAsync(integrationEvent);
                await db.SaveChangesAsync();
            }

            var processor = provider.GetRequiredService<OutboxMessageProcessor>();
            var processMethod = typeof(OutboxMessageProcessor)
                .GetMethod("ProcessBatchAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

            await (Task)processMethod.Invoke(processor, new object?[] { CancellationToken.None })!;

            using var verificationScope = provider.CreateScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();
            var processedMessage = await verificationDb.OutboxMessages.FirstAsync();

            processedMessage.ProcessedAt.Should().NotBeNull();
            processedMessage.Error.Should().BeNull();
            published.Should().ContainSingle();
        }

        [Fact]
        public async Task Inbox_prevents_duplicate_processing()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddSingleton<IDomainEventDispatcher, NoOpDispatcher>();
            services.AddDbContext<FlowOpsDbContext>(opt => opt.UseSqlServer(connection));
            services.AddScoped<IIntegrationEventInbox, IntegrationEventInBox>();

            var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();
                await db.Database.EnsureCreatedAsync();
            }

            var evt = new SubscriptionActivatedEvent
            {
                SubscriptionId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                PlanCode = "BASIC"
            };

            var processed = 0;
            using (var scope = provider.CreateScope())
            {
                var inbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventInbox>();
                await inbox.ProcessAsync("consumer-a", evt, _ =>
                {
                    processed++;
                    return Task.CompletedTask;
                });
            }

            using (var scope = provider.CreateScope())
            {
                var inbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventInbox>();
                await inbox.ProcessAsync("consumer-a", evt, _ =>
                {
                    processed++;
                    return Task.CompletedTask;
                });
            }

            processed.Should().Be(1);
        }

        private static FlowOpsDbContext CreateContext(SqliteConnection connection, IDomainEventDispatcher dispatcher)
        {
            var options = new DbContextOptionsBuilder<FlowOpsDbContext>()
                .UseSqlServer(connection)
                .Options;

            return new FlowOpsDbContext(options, dispatcher, NullLogger<FlowOpsDbContext>.Instance);
        }

        private sealed class RecordingDispatcher : IDomainEventDispatcher
        {
            private readonly IList<IDomainEvent> _events;

            public RecordingDispatcher(IList<IDomainEvent> events)
            {
                _events = events;
            }

            public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            {
                foreach (var domainEvent in domainEvents)
                {
                    _events.Add(domainEvent);
                }

                return Task.CompletedTask;
            }
        }

        private sealed class NoOpDispatcher : IDomainEventDispatcher
        {
            public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
