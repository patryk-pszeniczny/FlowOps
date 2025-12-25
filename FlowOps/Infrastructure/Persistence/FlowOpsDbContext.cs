using FlowOps.BuildingBlocks.Diagnostics;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Domain.Customers;
using FlowOps.Domain.Events;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Persistence.Entities;
using FlowOps.Infrastructure.Persistence.Inbox;
using FlowOps.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence
{
    public sealed class FlowOpsDbContext : DbContext
    {
        private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly ILogger<FlowOpsDbContext> _logger;
        public FlowOpsDbContext(
            DbContextOptions<FlowOpsDbContext> options,
            IDomainEventDispatcher domainEventDispatcher,
            ILogger<FlowOpsDbContext> logger)
            : base(options)
        {
            _domainEventDispatcher = domainEventDispatcher;
            _logger = logger;
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<IdempotencyKeyEntity> IdempotencyKeys => Set<IdempotencyKeyEntity>();
        public DbSet<IntegrationEventEntity> IntegrationEvents => Set<IntegrationEventEntity>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
        public DbSet<CustomerDirectoryEntry> CustomerDirectoryEntries => Set<CustomerDirectoryEntry>();
        public DbSet<CustomerReport> CustomerReports => Set<CustomerReport>();
        public DbSet<ActiveSubscription> ActiveSubscriptions => Set<ActiveSubscription>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
        public DbSet<UserListCacheEntry> UserListCaches => Set<UserListCacheEntry>();
        public DbSet<SubscriptionDraft> SubscriptionDrafts => Set<SubscriptionDraft>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowOpsDbContext).Assembly);
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var started = DateTime.UtcNow;
            var domainEvents = ChangeTracker
                .Entries<IHasDomainEvents>()
                .SelectMany(entry => entry.Entity.DomainEvents)
                .ToList();

            foreach(var entity in ChangeTracker.Entries<IHasDomainEvents>())
            {
                entity.Entity.ClearDomainEvents();
            }
            if(domainEvents.Count > 0)
            {
                await _domainEventDispatcher
                    .DispatchAsync(domainEvents, cancellationToken)
                    .ConfigureAwait(false);
            }
            try
            {
                var result = await base.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                FlowOpsMetrics.DbContextSaveOperations.Add(1);
                FlowOpsMetrics.DbContextSaveDuration.Record((DateTime.UtcNow - started).TotalMilliseconds);

                _logger.LogInformation("Persisted {ChangeCount} changes and dispatched {DomainEventCount} domain events.", ChangeTracker.Entries().Count(e => e.State != EntityState.Unchanged), domainEvents.Count);

                return result;
            }
            catch (Exception ex)
            {
                FlowOpsMetrics.DbContextSaveFailures.Add(1);
                _logger.LogError(ex, "FlowOpsDbContext.SaveChangesAsync failed after {DurationMs} ms.", (DateTime.UtcNow - started).TotalMilliseconds);
                throw;
            }
        }
    }
}