using System.Text.Json;
using FlowOps.Application.UserData;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Repositories;
using FlowOps.Infrastructure.Persistence.UserData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.Tests.Infrastructure.UserData;

public class UserStateInitializerTests
{
    private static FlowOpsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FlowOpsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FlowOpsDbContext(options, new NoOpDispatcher(), NullLogger<FlowOpsDbContext>.Instance);
    }

    [Fact]
    public async Task Seeds_default_preferences_when_empty()
    {
        using var dbContext = CreateDbContext();
        var repository = new EfUserStateRepository(dbContext);
        var service = new UserStateService(repository, NullLogger<UserStateService>.Instance);
        var initializer = new UserStateInitializer(service, new InMemoryLegacyReader(new LegacyLocalStorageSnapshot()), NullLogger<UserStateInitializer>.Instance);

        await initializer.InitializeAsync();

        var snapshot = await service.GetAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        snapshot.Preferences.Should().ContainEquivalentOf(new UserPreferenceDto("language", "pl-PL", snapshot.Preferences.First(p => p.Key == "language").UpdatedAt));
        snapshot.Preferences.Should().ContainEquivalentOf(new UserPreferenceDto("theme", "light", snapshot.Preferences.First(p => p.Key == "theme").UpdatedAt));
        snapshot.CachedLists.Should().ContainSingle(x => x.CacheKey == "plans");
    }

    [Fact]
    public async Task Migrates_legacy_snapshot_into_database()
    {
        var userId = Guid.NewGuid();
        var legacy = new LegacyLocalStorageSnapshot
        {
            Preferences = new[]
            {
                new LegacyUserPreference(userId, "locale", "en-US", DateTime.UtcNow.AddMinutes(-5)),
                new LegacyUserPreference(userId, "theme", "dark", DateTime.UtcNow)
            },
            CachedLists = new[]
            {
                new LegacyCachedList(userId, "customers", "[{\"id\":1}]", DateTime.UtcNow, DateTime.UtcNow.AddDays(1))
            },
            Drafts = new[]
            {
                new LegacySubscriptionDraft(Guid.NewGuid(), userId, Guid.NewGuid(), "BASIC", JsonSerializer.Serialize(new { note = "draft" }), DateTime.UtcNow)
            }
        };

        using var dbContext = CreateDbContext();
        var repository = new EfUserStateRepository(dbContext);
        var service = new UserStateService(repository, NullLogger<UserStateService>.Instance);
        var initializer = new UserStateInitializer(service, new InMemoryLegacyReader(legacy), NullLogger<UserStateInitializer>.Instance);

        await initializer.InitializeAsync();

        var snapshot = await service.GetAsync(userId);

        snapshot.Preferences.Should().HaveCount(2);
        snapshot.Preferences.Should().ContainEquivalentOf(new UserPreferenceDto("theme", "dark", snapshot.Preferences.First(p => p.Key == "theme").UpdatedAt));
        snapshot.CachedLists.Should().ContainSingle(x => x.CacheKey == "customers");
        snapshot.Drafts.Should().ContainSingle();
    }

    private sealed class NoOpDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

    }

    private sealed class InMemoryLegacyReader : ILegacyLocalStorageReader
    {
        private readonly LegacyLocalStorageSnapshot _snapshot;

        public InMemoryLegacyReader(LegacyLocalStorageSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<LegacyLocalStorageSnapshot> ReadAsync(CancellationToken ct = default) => Task.FromResult(_snapshot);
    }
}