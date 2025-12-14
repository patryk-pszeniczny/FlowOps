

using FlowOps.Infrastructure.Customers;
using FlowOps.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Sql
{
    public class FlowOpsDbContext : DbContext
    {
        public FlowOpsDbContext(DbContextOptions<FlowOpsDbContext> options)
            : base(options)
        {
        }

        public DbSet<IdempotencyKeyEntity> IdempotencyKeys => Set<IdempotencyKeyEntity>();
        public DbSet<IntegrationEventEntity> IntegrationEvents => Set<IntegrationEventEntity>();

        public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdempotencyKeyEntity>(entity =>
            {
                entity.ToTable("IdempotencyKeys");

                entity.HasKey(e => e.Key);

                entity.Property(e => e.Key)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.SubscriptionId)
                    .IsRequired();
            });

            modelBuilder.Entity<IntegrationEventEntity>(entity =>
            {
                entity.ToTable("IntegrationEvents");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.TypeName)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(e => e.OccurredAt)
                    .IsRequired();

                entity.Property(e => e.Version)
                    .IsRequired();

                entity.Property(e => e.PayLoadJson)
                    .IsRequired();
            });

            modelBuilder.Entity<CustomerEntity>(entity =>
            {
                entity.ToTable("Customers");

                entity.HasKey(e => e.CustomerId);

                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.TaxId)
                    .HasMaxLength(32);

                entity.Property(e => e.Email)
                    .HasMaxLength(256);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();
            });
        }
    }
}
