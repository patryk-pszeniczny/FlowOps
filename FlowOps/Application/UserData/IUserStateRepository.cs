namespace FlowOps.Application.UserData
{
    public interface IUserStateRepository
    {
        Task<UserStateSnapshot> GetSnapshotAsync(Guid userId, CancellationToken ct = default);
        Task UpsertPreferenceAsync(Guid userId, string key, string value, DateTime updatedAt, CancellationToken ct = default);
        Task SaveDraftAsync(Guid draftId, Guid userId, Guid? customerId, string? planCode, string payload, DateTime updatedAt, CancellationToken ct = default);
        Task UpsertCachedListAsync(Guid userId, string cacheKey, string payload, DateTime cachedAt, DateTime? expiresAt, CancellationToken ct = default);
        Task BulkImportAsync(LegacyLocalStorageSnapshot snapshot, CancellationToken ct = default);
    }
}
