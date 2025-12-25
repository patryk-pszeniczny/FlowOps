using FlowOps.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
    {
        public void Configure(EntityTypeBuilder<InboxMessage> builder)
        {
            builder.ToTable("InboxMessages");

            builder.HasKey(x => new { x.Consumer, x.EventId });

            builder.Property(x => x.Consumer)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.ProcessedAt)
                .IsRequired();

            builder.HasIndex(x => x.ProcessedAt);
        }
    }
}