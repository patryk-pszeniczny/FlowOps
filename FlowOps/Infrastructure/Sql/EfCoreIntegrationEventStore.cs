using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FlowOps.Infrastructure.Sql
{
    public sealed class EfCoreIntegrationEventStore : IIntegrationEventStore
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EfCoreIntegrationEventStore> _logger;

        public EfCoreIntegrationEventStore(
            IServiceScopeFactory scopeFactory,
            ILogger<EfCoreIntegrationEventStore> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task AppendAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            if(@event is null)
            {
                throw new ArgumentNullException(nameof(@event));
            }
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var _dbContex = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();

                var entity = new IntegrationEventEntity
                {
                    Id = @event.Id,
                    TypeName = @event.GetType().FullName ?? @event.GetType().Name,
                    OccurredAt = @event.OccurredOn,
                    Version = @event.Version,
                    PayLoadJson = JsonSerializer.Serialize(@event, @event.GetType())
                };
                _dbContex.IntegrationEvents.Add(entity);
                await _dbContex.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Stored integration event {EventType} with Id={EventId}, Version={Version}.",
                    entity.TypeName,
                    entity.Id,
                    entity.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while storing integration event {EventType} with Id={EventId}.",
                    @event.GetType().FullName,
                    @event.Id);
                throw;
            }
        }

        public async Task<IReadOnlyList<IntegrationEvent>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();

                var entities = await dbContext.IntegrationEvents
                    .OrderBy(e => e.OccurredAt)
                    .ThenBy(e => e.Id)
                    .ToListAsync(cancellationToken);

                var result = new List<IntegrationEvent>(entities.Count);

                foreach (var entity in entities)
                {
                    var type = ResolveEventType(entity.TypeName);
                    if (type is null)
                    {
                        _logger.LogWarning(
                            "Could not resolve type '{TypeName}' while reading integration event Id={EventId}. Skipping.",
                            entity.TypeName,
                            entity.Id);
                        continue;
                    }

                    try
                    {
                        var deserialized = (IntegrationEvent?)JsonSerializer.Deserialize(
                            entity.PayLoadJson,
                            type);

                        if (deserialized is null)
                        {
                            _logger.LogWarning(
                                "Deserialized null for integration event Id={EventId}, Type={TypeName}. Skipping.",
                                entity.Id,
                                entity.TypeName);
                            continue;
                        }

                        result.Add(deserialized);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error while deserializing integration event Id={EventId}, Type={TypeName}. Skipping.",
                            entity.Id,
                            entity.TypeName);
                    }
                }

                _logger.LogInformation(
                    "Loaded {Count} integration events from store.",
                    result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while loading integration events from store.");
                throw;
            }
        }
        private static Type? ResolveEventType(string typeName)
        {
            var type = Type.GetType(typeName, throwOnError: false);
            if (type is not null)
            {
                return type;
            }
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName, throwOnError: false);
                if (type is not null)
                    return type;
            }

            return null;
        }
    }
}
