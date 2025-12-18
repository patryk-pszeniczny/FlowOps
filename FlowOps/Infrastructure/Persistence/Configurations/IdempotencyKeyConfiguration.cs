using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKeyEntity>
    {
        public void Configure(EntityTypeBuilder<IdempotencyKeyEntity> builder)
        {
            builder.ToTable("IdempotencyKeys");

            builder.HasKey(x => x.Key);

            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.SubscriptionId)
                .IsRequired();
        }
    }
}