using FlowOps.Domain.Customers;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence
{
    public sealed class FlowOpsDbContext : DbContext
    {
        public FlowOpsDbContext(DbContextOptions<FlowOpsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<IdempotencyKeyEntity> IdempotencyKeys => Set<IdempotencyKeyEntity>();
        public DbSet<IntegrationEventEntity> IntegrationEvents => Set<IntegrationEventEntity>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
        public DbSet<CustomerDirectoryEntry> CustomerDirectoryEntries => Set<CustomerDirectoryEntry>();
        public DbSet<CustomerReport> CustomerReports => Set<CustomerReport>();
        public DbSet<ActiveSubscription> ActiveSubscriptions => Set<ActiveSubscription>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowOpsDbContext).Assembly);
        }
    }
}