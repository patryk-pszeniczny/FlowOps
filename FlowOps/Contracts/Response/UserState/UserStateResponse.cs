namespace FlowOps.Contracts.Response.UserState
{
    public sealed class UserStateResponse
    {
        public required Guid UserId { get; init; }
        public required IReadOnlyCollection<UserPreferenceItem> Preferences { get; init; }
        public required IReadOnlyCollection<CachedListItem> CachedLists { get; init; }
        public required IReadOnlyCollection<SubscriptionDraftItem> Drafts { get; init; }
    }
    public sealed class UserPreferenceItem
    {
        public required string Key { get; init; }
        public required string Value { get; init; }
        public required DateTime UpdatedAt { get; init; }
    }
    public sealed class CachedListItem 
    { 
        public required string CacheKey { get; init; }
        public required string Payload { get; init; }
        public required DateTime CachedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
    }
    public sealed class SubscriptionDraftItem
    {
        public required Guid DraftId { get; init; }
        public Guid? CustomerId { get; init; }
        public string? PlanCode { get; init; }
        public required string Payload { get; init; }
        public required DateTime LastUpdatedAt { get; init; }
    }
}
