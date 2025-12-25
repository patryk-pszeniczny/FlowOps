using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class SubscriptionDraftConfiguration : IEntityTypeConfiguration<SubscriptionDraft>
    {
        public void Configure(EntityTypeBuilder<SubscriptionDraft> builder)
        {
            builder.ToTable("SubscriptionDrafts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.PlanCode)
                .HasMaxLength(100);

            builder.Property(x => x.Payload)
                .IsRequired();

            builder.Property(x => x.LastUpdatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.PlanCode });
        }
    }
}