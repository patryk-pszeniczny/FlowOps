using FlowOps.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.TaxId)
                .HasMaxLength(32);

            builder.Property(c => c.Email)
                .HasMaxLength(256);

            builder.Property(c => c.CreatedAt)
                .IsRequired();
        }
    }
}