namespace FlowOps.Application.UserData
{
    public sealed record UserPreferenceDto(string Key, string Value, DateTime UpdatedAt);
    public sealed record CachedListEntryDto(string CacheKey, string Payload, DateTime CachedAt, DateTime? ExpiresAt);
    public sealed record SubscriptionDraftDto(Guid Id, Guid? CustomerId, string? PlanCode, string Payload, DateTime LastUpdatedAt);
    public sealed class UserStateSnapshot
    {
        public Guid UserId { get; init; }
        public IReadOnlyList<UserPreferenceDto> Preferences { get; init; } = Array.Empty<UserPreferenceDto>();
        public IReadOnlyList<CachedListEntryDto> CachedLists { get; init; } = Array.Empty<CachedListEntryDto>();
        public IReadOnlyList<SubscriptionDraftDto> Drafts { get; init; } = Array.Empty<SubscriptionDraftDto>();
    }

    public sealed record LegacyUserPreference(Guid UserId, string Key, string Value, DateTime UpdatedAt);
    public sealed record LegacyCachedList(Guid UserId, string CacheKey, string Payload, DateTime CachedAt, DateTime? ExpiresAt);
    public sealed record LegacySubscriptionDraft(Guid Id, Guid UserId, Guid? CustomerId, string? PlanCode, string Payload, DateTime LastUpdatedAt);
    public sealed class LegacyLocalStorageSnapshot
    {
        public IReadOnlyCollection<LegacyUserPreference> Preferences { get; init; } = Array.Empty<LegacyUserPreference>();
        public IReadOnlyCollection<LegacyCachedList> CachedLists { get; init; } = Array.Empty<LegacyCachedList>();
        public IReadOnlyCollection<LegacySubscriptionDraft> Drafts { get; init; } = Array.Empty<LegacySubscriptionDraft>();
    }
}
