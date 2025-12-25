namespace FlowOps.Application.UserData
{
    public sealed class UserStateService
    {
        private readonly IUserStateRepository _repository;
        private readonly ILogger<UserStateService> _logger;

        private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public UserStateService(IUserStateRepository repository, ILogger<UserStateService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        public Task<UserStateSnapshot> GetAsync(Guid userId, CancellationToken ct = default) =>
            _repository.GetSnapshotAsync(userId, ct);

        public Task SavePreferenceAsync(Guid userId, string key, string value, DateTime updatedAt, CancellationToken ct = default) =>
            _repository.UpsertPreferenceAsync(userId, key, value, updatedAt, ct);

        public Task SaveDraftAsync(Guid draftId, Guid userId, Guid? customerId, string? planCode, string payload, DateTime updatedAt,  CancellationToken ct = default) =>
            _repository.SaveDraftAsync(draftId, userId, customerId, planCode, payload, updatedAt, ct);

        public Task SaveListCacheAsync(Guid userId, string cacheKey, string payload, DateTime cachedAt, DateTime? expiresAt, CancellationToken ct = default) =>
            _repository.UpsertCachedListAsync(userId, cacheKey, payload, cachedAt, expiresAt, ct);

        public async Task SeedDefaultsAsync(CancellationToken ct = default)
        {
            var snapshot = await _repository.GetSnapshotAsync(DefaultUserId, ct);
            if(snapshot.Preferences.Count > 0)
            {
                return;
            }

            _logger.LogInformation("Seeding default user preferences for system user {UserId}.", DefaultUserId);

            var now = DateTime.UtcNow;
            await _repository.UpsertPreferenceAsync(DefaultUserId, "language", "pl-PL", now, ct);
            await _repository.UpsertPreferenceAsync(DefaultUserId, "theme", "light", now, ct);
            await _repository.UpsertCachedListAsync(DefaultUserId, "plans", "[]", now, now.AddHours(1), ct);
        }
        public async Task MigrateAsync(LegacyLocalStorageSnapshot snapshot, CancellationToken ct = default)
        {
            if(snapshot is null)
            {
                return;
            }
            await _repository.BulkImportAsync(snapshot, ct);
            _logger.LogInformation("Migrated legacy user data: {Preferences} preferences, {CachedLists} cached lists, {Drafts} drafts.",
                snapshot.Preferences.Count,
                snapshot.CachedLists.Count,
                snapshot.Drafts.Count);
        }
    }
}
