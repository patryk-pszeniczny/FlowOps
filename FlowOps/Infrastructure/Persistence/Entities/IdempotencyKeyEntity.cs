namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class IdempotencyKeyEntity
    {
        public string Key { get; set; } = string.Empty;
        public Guid SubscriptionId { get; set; }
    }
}
