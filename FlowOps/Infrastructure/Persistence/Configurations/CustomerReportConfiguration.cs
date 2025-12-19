
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class CustomerReportConfiguration : IEntityTypeConfiguration<CustomerReport>
    {
        public void Configure(EntityTypeBuilder<CustomerReport> builder)
        {
            builder.ToTable("CustomerReports");

            builder.HasKey(x => x.CustomerId);

            builder.Property(x => x.CustomerId)
                .ValueGeneratedNever();

            builder.Property(x => x.ActiveSubscriptions)
                .IsRequired();

            builder.Property(x => x.TotalInvoiced)
                .HasPrecision(18, 2);

            builder.Property(x => x.TotalPaid)
                .HasPrecision(18, 2);
        }
    }
}