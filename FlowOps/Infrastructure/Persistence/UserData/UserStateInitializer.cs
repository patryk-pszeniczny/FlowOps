using FlowOps.Application.UserData;

namespace FlowOps.Infrastructure.Persistence.UserData
{
    public sealed class UserStateInitializer
    {
        private readonly UserStateService _service;
        private readonly ILegacyLocalStorageReader _reader;
        private readonly ILogger<UserStateInitializer> _logger;

        public UserStateInitializer(UserStateService service, ILegacyLocalStorageReader reader, ILogger<UserStateInitializer> logger)
        {
            _service = service;
            _reader = reader;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await _service.SeedDefaultsAsync(ct);

            var snapshot = await _reader.ReadAsync(ct);
            if (snapshot.Preferences.Count == 0 && snapshot.CachedLists.Count == 0 && snapshot.Drafts.Count == 0)
            {
                _logger.LogInformation("No legacy local storage snapshot to migrate.");
                return;
            }

            await _service.MigrateAsync(snapshot, ct);
        }
    }
}
