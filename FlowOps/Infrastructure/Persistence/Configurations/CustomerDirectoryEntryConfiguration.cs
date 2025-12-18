using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class CustomerDirectoryEntryConfiguration : IEntityTypeConfiguration<CustomerDirectoryEntry>
    {
        public void Configure(EntityTypeBuilder<CustomerDirectoryEntry> builder)
        {
            builder.ToTable("CustomerDirectory");

            builder.HasKey(x => x.CustomerId);

            builder.Property(x => x.CustomerId)
                .ValueGeneratedNever();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.TaxId)
                .HasMaxLength(32);

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}