using FlowOps.Domain.Customers;
using FlowOps.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable("Subscriptions");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Id)
                .ValueGeneratedNever();

            builder.Property(s => s.CustomerId)
                .IsRequired();

            builder.Property(s => s.PlanCode)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(24);

            builder.Property(s => s.ActivatedAt);
            builder.Property(s => s.ExpiresAt);
            builder.Property(s => s.CancelledAt);
            builder.Property(s => s.SuspendedAt);
            builder.Property(s => s.ResumedAt);

            builder.HasIndex(s => s.CustomerId);

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}