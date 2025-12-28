using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Domain.Events;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Subscriptions;
using FlowOps.Domain.Subscriptions.Events;
using FlowOps.Events;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Inbox;
using FlowOps.Infrastructure.Persistence.Outbox;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data.Common;
using System.Reflection;
using Xunit;

namespace FlowOps.Tests.Infrastructure.Persistence
{
    public class FlowOpsDbContextTests
    {
        [Fact]
        public async Task SaveChanges_dispatches_domain_events_and_saves_entities()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await EnableForeignKeysAsync(connection);

            var dispatched = new List<IDomainEvent>();
            using var context = CreateContext(connection, new RecordingDispatcher(dispatched));
            await context.Database.EnsureCreatedAsync();

            var customerId = await SeedCustomerRowAsync(context);

            var subscription = Subscription.Create(customerId, "basic");
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
            await EnableForeignKeysAsync(connection);

            var services = new ServiceCollection();
            services.AddSingleton<IDomainEventDispatcher, NoOpDispatcher>();
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            services.AddLogging();
            services.AddDbContext<FlowOpsDbContext>(opt => opt.UseSqlite(connection));
            services.AddScoped<IOutboxMessageWriter, OutboxMessageWriter>();
            services.AddSingleton<OutboxMessageProcessor>();

            var provider = services.BuildServiceProvider();

            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();
                await db.Database.EnsureCreatedAsync();
            }

            var bus = (InMemoryEventBus)provider.GetRequiredService<IEventBus>();
            var tcs = new TaskCompletionSource<SubscriptionActivatedEvent>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            bus.Subscribe<SubscriptionActivatedEvent>(evt =>
            {
                tcs.TrySetResult(evt);
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

            var publishedEvent = await WaitWithTimeoutAsync(tcs.Task, TimeSpan.FromSeconds(2));
            publishedEvent.Should().NotBeNull();
        }

        [Fact]
        public async Task Inbox_prevents_duplicate_processing()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            await EnableForeignKeysAsync(connection);

            var services = new ServiceCollection();
            services.AddSingleton<IDomainEventDispatcher, NoOpDispatcher>();
            services.AddLogging();
            services.AddDbContext<FlowOpsDbContext>(opt => opt.UseSqlite(connection));
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
                .UseSqlite(connection)
                .Options;

            return new FlowOpsDbContext(options, dispatcher, NullLogger<FlowOpsDbContext>.Instance);
        }

        private static async Task EnableForeignKeysAsync(SqliteConnection connection)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            await cmd.ExecuteNonQueryAsync();
        }
        private static async Task<Guid> SeedCustomerRowAsync(FlowOpsDbContext context)
        {
            var customerId = Guid.NewGuid();

            await EnsureDbConnectionOpenAsync(context);

            var tableName = await ResolveCustomerTableNameAsync(context);

            var columns = await ReadTableInfoAsync(context, tableName);

            var now = DateTime.UtcNow;
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = customerId,
                ["CustomerId"] = customerId,
                ["Name"] = "Test Customer",
                ["TaxId"] = "TAX-TEST",
                ["Email"] = "customer@example.com",
                ["CreatedAt"] = now,
                ["CreatedOn"] = now
            };

            foreach (var col in columns)
            {
                if (!col.NotNull || col.HasDefault || col.IsPk)
                    continue;

                if (values.ContainsKey(col.Name))
                    continue;

                values[col.Name] = GetSafeDefaultValue(col);
            }

            var insertCols = columns.Where(c => values.ContainsKey(c.Name)).ToList();
            if (insertCols.Count == 0)
                throw new InvalidOperationException($"Nie udało się zbudować INSERT dla tabeli '{tableName}' (brak dopasowanych kolumn).");

            var colList = string.Join(", ", insertCols.Select(c => $"\"{c.Name}\""));
            var paramList = string.Join(", ", insertCols.Select((_, i) => $"@p{i}"));

            var sql = $"INSERT INTO \"{tableName}\" ({colList}) VALUES ({paramList});";

            await using var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;

            for (var i = 0; i < insertCols.Count; i++)
            {
                var col = insertCols[i];
                var param = cmd.CreateParameter();
                param.ParameterName = $"@p{i}";
                param.Value = ToSqliteValue(col, values[col.Name]) ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }

            await cmd.ExecuteNonQueryAsync();

            return customerId;
        }

        private static async Task EnsureDbConnectionOpenAsync(FlowOpsDbContext context)
        {
            var conn = context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await context.Database.OpenConnectionAsync();
            }
        }

        private static async Task<string> ResolveCustomerTableNameAsync(FlowOpsDbContext context)
        {
            var exact = await TryFindSingleAsync(context, "SELECT name FROM sqlite_master WHERE type='table' AND lower(name) = 'customers';");
            if (!string.IsNullOrWhiteSpace(exact))
                return exact;

            var alt = await TryFindSingleAsync(context, "SELECT name FROM sqlite_master WHERE type='table' AND lower(name) LIKE '%customer%';");
            if (string.IsNullOrWhiteSpace(alt))
                throw new InvalidOperationException("Nie znaleziono tabeli Customer/Customers w SQLite schema.");

            return alt;
        }

        private static async Task<string?> TryFindSingleAsync(FlowOpsDbContext context, string sql)
        {
            await using var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? reader.GetString(0) : null;
        }

        private static async Task<IReadOnlyList<SqliteColumnInfo>> ReadTableInfoAsync(FlowOpsDbContext context, string tableName)
        {
            await using var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            var result = new List<SqliteColumnInfo>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(1);
                var type = reader.IsDBNull(2) ? null : reader.GetString(2);
                var notNull = reader.GetInt32(3) == 1;
                var hasDefault = !reader.IsDBNull(4);
                var isPk = reader.GetInt32(5) == 1;

                result.Add(new SqliteColumnInfo(name, type, notNull, hasDefault, isPk));
            }

            return result;
        }

        private static object? GetSafeDefaultValue(SqliteColumnInfo col)
        {
            var t = col.Type ?? string.Empty;

            if (t.Contains("INT", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (t.Contains("REAL", StringComparison.OrdinalIgnoreCase) || t.Contains("FLOA", StringComparison.OrdinalIgnoreCase) || t.Contains("DOUB", StringComparison.OrdinalIgnoreCase))
                return 0.0;

            if (t.Contains("BLOB", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<byte>();

            if (t.Contains("DATE", StringComparison.OrdinalIgnoreCase) || t.Contains("TIME", StringComparison.OrdinalIgnoreCase) || col.Name.EndsWith("At", StringComparison.OrdinalIgnoreCase))
                return DateTime.UtcNow;

            if (col.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                return Guid.NewGuid();

            return "TEST";
        }

        private static object? ToSqliteValue(SqliteColumnInfo col, object? value)
        {
            if (value is null)
                return null;

            if (value is Guid g)
            {
                if (!string.IsNullOrWhiteSpace(col.Type) && col.Type!.Contains("BLOB", StringComparison.OrdinalIgnoreCase))
                    return g.ToByteArray();

                return g.ToString();
            }

            if (value is DateTime dt)
                return dt.ToString("O");

            return value;
        }

        private sealed record SqliteColumnInfo(string Name, string? Type, bool NotNull, bool HasDefault, bool IsPk);

        private static async Task<T> WaitWithTimeoutAsync<T>(Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var completed = await Task.WhenAny(task, Task.Delay(Timeout.InfiniteTimeSpan, cts.Token));
            if (completed != task)
                throw new TimeoutException($"Timeout po {timeout.TotalSeconds:0.##}s: event nie został opublikowany na busie.");

            return await task;
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
            public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }
}
