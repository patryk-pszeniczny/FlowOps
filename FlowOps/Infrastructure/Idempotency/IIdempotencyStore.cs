namespace FlowOps.Infrastructure.Idempotency
{
    public interface IIdempotencyStore
    {
        bool TryGet(string key, out Guid subscriptionId);
        void Set(string key, Guid subscriptionId);
    }
}
