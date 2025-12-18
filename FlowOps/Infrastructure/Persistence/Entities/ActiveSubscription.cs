namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class ActiveSubscription
    {
        public Guid CustomerId { get; set; }
        public Guid SubscriptionId { get; set; }
    }
}
