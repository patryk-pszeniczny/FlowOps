namespace FlowOps.Contracts.Request.Idempotency
{
    public sealed class UpsertIdempotencyKeyRequest
    {
        public string Key { get; init; } = string.Empty;
        public Guid SubscriptionId { get; init; }
    }
}
