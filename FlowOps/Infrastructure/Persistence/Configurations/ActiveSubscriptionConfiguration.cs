using FlowOps.Domain.Customers;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class ActiveSubscriptionConfiguration : IEntityTypeConfiguration<ActiveSubscription>
    {
        public void Configure(EntityTypeBuilder<ActiveSubscription> builder)
        {
            builder.ToTable("ActiveSubscriptionIds");

            builder.HasKey(x => new { x.CustomerId, x.SubscriptionId });

            builder.HasIndex(x => x.SubscriptionId);

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<Subscription>()
                .WithMany()
                .HasForeignKey(x => x.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}