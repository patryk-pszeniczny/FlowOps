

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
        }
    }
}
