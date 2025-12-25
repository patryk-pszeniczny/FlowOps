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
        public FlowOpsDbContext(DbContextOptions<FlowOpsDbContext> options, IDomainEventDispatcher domainEventDispatcher)
            : base(options)
        {
            _domainEventDispatcher = domainEventDispatcher;
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
            return await base.SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}