using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class IntegrationEventConfiguration : IEntityTypeConfiguration<IntegrationEventEntity>
    {
        public void Configure(EntityTypeBuilder<IntegrationEventEntity> builder)
        {
            builder.ToTable("IntegrationEvents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TypeName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.OccurredAt)
                .IsRequired();

            builder.Property(x => x.Version)
                .IsRequired();

            builder.Property(x => x.PayLoadJson)
                .IsRequired();
        }
    }
}