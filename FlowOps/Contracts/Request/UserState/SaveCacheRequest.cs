namespace FlowOps.Contracts.Request.UserState
{
    public sealed class SaveCacheRequest
    {
        public required string CacheKey { get; init; }
        public required string Payload { get; init; }
        public DateTime? ExpiresAt { get; init; }
    }
}
