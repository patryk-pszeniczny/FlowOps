using FlowOps.Application.UserData;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FlowOps.Infrastructure.Persistence.UserData
{
    public interface ILegacyLocalStorageReader
    {
        Task<LegacyLocalStorageSnapshot> ReadAsync(CancellationToken ct = default);
    }

    public sealed class FileLegacyLocalStorageReader : ILegacyLocalStorageReader
    {
        private readonly LegacyLocalStorageOptions _options;
        private readonly ILogger<FileLegacyLocalStorageReader> _logger;

        public FileLegacyLocalStorageReader(IOptions<LegacyLocalStorageOptions> options, ILogger<FileLegacyLocalStorageReader> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<LegacyLocalStorageSnapshot> ReadAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.SnapshotFilePath))
            {
                return new LegacyLocalStorageSnapshot();
            }

            if (!File.Exists(_options.SnapshotFilePath))
            {
                _logger.LogInformation("Legacy local storage snapshot not found at {Path}", _options.SnapshotFilePath);
                return new LegacyLocalStorageSnapshot();
            }

            await using var stream = File.OpenRead(_options.SnapshotFilePath);
            var payload = await JsonSerializer.DeserializeAsync<LegacyLocalStorageSnapshot>(stream, cancellationToken: ct)
                ?? new LegacyLocalStorageSnapshot();

            _logger.LogInformation("Loaded legacy local storage snapshot from {Path}", _options.SnapshotFilePath);
            return payload;
        }
    }
}
