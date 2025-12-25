using FlowOps.Application.UserData;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence.Repositories
{
    public sealed class EfUserStateRepository : IUserStateRepository
    {
        private readonly FlowOpsDbContext _dbContext;

        public EfUserStateRepository(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserStateSnapshot> GetSnapshotAsync(Guid userId, CancellationToken ct = default)
        {
            var preferences = await _dbContext.UserPreferences
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Key)
                .Select(x => new UserPreferenceDto(x.Key, x.Value, x.UpdatedAt))
                .ToListAsync(ct);

            var caches = await _dbContext.UserListCaches
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CachedAt)
                .Select(x => new CachedListEntryDto(x.CacheKey, x.Payload, x.CachedAt, x.ExpiresAt))
                .ToListAsync(ct);

            var drafts = await _dbContext.SubscriptionDrafts
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.LastUpdatedAt)
                .Select(x => new SubscriptionDraftDto(x.Id, x.CustomerId, x.PlanCode, x.Payload, x.LastUpdatedAt))
                .ToListAsync(ct);

            return new UserStateSnapshot
            {
                UserId = userId,
                Preferences = preferences,
                CachedLists = caches,
                Drafts = drafts
            };
        }

        public async Task UpsertPreferenceAsync(Guid userId, string key, string value, DateTime updatedAt, CancellationToken ct = default)
        {
            var existing = await _dbContext.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId && x.Key == key, ct);
            if (existing is null)
            {
                existing = new UserPreference
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Key = key,
                    Value = value,
                    UpdatedAt = updatedAt
                };
                await _dbContext.UserPreferences.AddAsync(existing, ct);
            }
            else
            {
                existing.Value = value;
                existing.UpdatedAt = updatedAt;
                _dbContext.UserPreferences.Update(existing);
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task SaveDraftAsync(Guid draftId, Guid userId, Guid? customerId, string? planCode, string payload, DateTime updatedAt, CancellationToken ct = default)
        {
            var existing = await _dbContext.SubscriptionDrafts.FirstOrDefaultAsync(x => x.Id == draftId, ct);
            if (existing is null)
            {
                existing = new SubscriptionDraft
                {
                    Id = draftId,
                    UserId = userId,
                    CustomerId = customerId,
                    PlanCode = planCode,
                    Payload = payload,
                    LastUpdatedAt = updatedAt
                };
                await _dbContext.SubscriptionDrafts.AddAsync(existing, ct);
            }
            else
            {
                existing.CustomerId = customerId;
                existing.PlanCode = planCode;
                existing.Payload = payload;
                existing.LastUpdatedAt = updatedAt;
                _dbContext.SubscriptionDrafts.Update(existing);
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task UpsertCachedListAsync(Guid userId, string cacheKey, string payload, DateTime cachedAt, DateTime? expiresAt, CancellationToken ct = default)
        {
            var existing = await _dbContext.UserListCaches.FirstOrDefaultAsync(x => x.UserId == userId && x.CacheKey == cacheKey, ct);
            if (existing is null)
            {
                existing = new UserListCacheEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CacheKey = cacheKey,
                    Payload = payload,
                    CachedAt = cachedAt,
                    ExpiresAt = expiresAt
                };
                await _dbContext.UserListCaches.AddAsync(existing, ct);
            }
            else
            {
                existing.Payload = payload;
                existing.CachedAt = cachedAt;
                existing.ExpiresAt = expiresAt;
                _dbContext.UserListCaches.Update(existing);
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task BulkImportAsync(LegacyLocalStorageSnapshot snapshot, CancellationToken ct = default)
        {
            if (snapshot.Preferences.Count == 0 && snapshot.CachedLists.Count == 0 && snapshot.Drafts.Count == 0)
            {
                return;
            }

            foreach (var pref in snapshot.Preferences)
            {
                var existing = await _dbContext.UserPreferences.FirstOrDefaultAsync(x => x.UserId == pref.UserId && x.Key == pref.Key, ct);
                if (existing is null)
                {
                    await _dbContext.UserPreferences.AddAsync(new UserPreference
                    {
                        Id = Guid.NewGuid(),
                        UserId = pref.UserId,
                        Key = pref.Key,
                        Value = pref.Value,
                        UpdatedAt = pref.UpdatedAt
                    }, ct);
                }
                else if (existing.UpdatedAt < pref.UpdatedAt)
                {
                    existing.Value = pref.Value;
                    existing.UpdatedAt = pref.UpdatedAt;
                    _dbContext.UserPreferences.Update(existing);
                }
            }

            foreach (var cache in snapshot.CachedLists)
            {
                var existing = await _dbContext.UserListCaches.FirstOrDefaultAsync(x => x.UserId == cache.UserId && x.CacheKey == cache.CacheKey, ct);
                if (existing is null)
                {
                    await _dbContext.UserListCaches.AddAsync(new UserListCacheEntry
                    {
                        Id = Guid.NewGuid(),
                        UserId = cache.UserId,
                        CacheKey = cache.CacheKey,
                        Payload = cache.Payload,
                        CachedAt = cache.CachedAt,
                        ExpiresAt = cache.ExpiresAt
                    }, ct);
                }
                else if (existing.CachedAt < cache.CachedAt)
                {
                    existing.Payload = cache.Payload;
                    existing.CachedAt = cache.CachedAt;
                    existing.ExpiresAt = cache.ExpiresAt;
                    _dbContext.UserListCaches.Update(existing);
                }
            }

            foreach (var draft in snapshot.Drafts)
            {
                var existing = await _dbContext.SubscriptionDrafts.FirstOrDefaultAsync(x => x.Id == draft.Id, ct);
                if (existing is null)
                {
                    await _dbContext.SubscriptionDrafts.AddAsync(new SubscriptionDraft
                    {
                        Id = draft.Id,
                        UserId = draft.UserId,
                        CustomerId = draft.CustomerId,
                        PlanCode = draft.PlanCode,
                        Payload = draft.Payload,
                        LastUpdatedAt = draft.LastUpdatedAt
                    }, ct);
                }
                else if (existing.LastUpdatedAt < draft.LastUpdatedAt)
                {
                    existing.CustomerId = draft.CustomerId;
                    existing.PlanCode = draft.PlanCode;
                    existing.Payload = draft.Payload;
                    existing.LastUpdatedAt = draft.LastUpdatedAt;
                    _dbContext.SubscriptionDrafts.Update(existing);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
